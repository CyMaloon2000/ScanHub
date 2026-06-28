using System;
using System.IO;
using System.Threading.Tasks;
using SQLite;
using InventoryScanner.Models;
using InventoryLocation = InventoryScanner.Models.Location;
using InventoryScanner.Helpers;
using InventoryScanner.Services.Interfaces;

namespace InventoryScanner.Services
{
    /// <summary>
    /// Manages SQLite database initialization, connection, and schema management.
    /// </summary>
    public class DatabaseService : IDatabaseService
    {
        private SQLiteAsyncConnection? _connection;
        private bool _isInitialized;
        private readonly SemaphoreSlim _initLock = new(1, 1);

        /// <summary>
        /// Gets the database file path.
        /// </summary>
        public string DatabasePath =>
            Path.Combine(FileSystem.AppDataDirectory, Constants.DatabaseName);

        /// <summary>
        /// Gets the SQLite connection, initializing if needed.
        /// </summary>
        public async Task<SQLiteAsyncConnection> GetConnectionAsync()
        {
            if (_isInitialized && _connection != null)
                return _connection;

            await _initLock.WaitAsync();
            try
            {
                if (_isInitialized && _connection != null)
                    return _connection;

                _connection = new SQLiteAsyncConnection(DatabasePath,
                    SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

                await _connection.ExecuteAsync("PRAGMA foreign_keys = ON");

                // Create tables only when they do not already exist.
                await EnsureTableAsync<Building>("Building");
                await EnsureTableAsync<InventoryLocation>("Location");
                await EnsureTableAsync<Room>("Room");
                await EnsureTableAsync<Office>("Office");
                await EnsureTableAsync<InventorySession>("InventoryScanSession");
                await EnsureScannedItemTableAsync();
                await EnsureTableAsync<User>("User");

                // Migration support for existing installs that predate normalized scope columns.
                await EnsureColumnAsync("InventoryScanSession", "BuildingId", "INTEGER NOT NULL DEFAULT 0");
                await EnsureColumnAsync("InventoryScanSession", "LocationId", "INTEGER NOT NULL DEFAULT 0");
                await EnsureColumnAsync("InventoryScanSession", "RoomId", "INTEGER NOT NULL DEFAULT 0");
                await EnsureColumnAsync("InventoryScanSession", "OfficeId", "INTEGER");

                // Create unique composite index for duplicate prevention
                await _connection.ExecuteAsync(
                    "CREATE UNIQUE INDEX IF NOT EXISTS IX_ScannedItem_Session_Identifier " +
                    "ON ScannedItem(SessionId, UniqueIdentifier)");

                // Create additional performance indexes
                await _connection.ExecuteAsync(
                    "CREATE INDEX IF NOT EXISTS IX_ScannedItem_SessionId " +
                    "ON ScannedItem(SessionId)");

                await _connection.ExecuteAsync(
                    "CREATE INDEX IF NOT EXISTS IX_ScannedItem_UniqueIdentifier " +
                    "ON ScannedItem(UniqueIdentifier)");

                await _connection.ExecuteAsync(
                    "CREATE INDEX IF NOT EXISTS IX_Location_BuildingId " +
                    "ON Location(BuildingId)");

                await _connection.ExecuteAsync(
                    "CREATE INDEX IF NOT EXISTS IX_Room_LocationId " +
                    "ON Room(LocationId)");

                await _connection.ExecuteAsync(
                    "CREATE INDEX IF NOT EXISTS IX_InventoryScanSession_BuildingId " +
                    "ON InventoryScanSession(BuildingId)");

                await _connection.ExecuteAsync(
                    "CREATE INDEX IF NOT EXISTS IX_InventoryScanSession_LocationId " +
                    "ON InventoryScanSession(LocationId)");

                await _connection.ExecuteAsync(
                    "CREATE INDEX IF NOT EXISTS IX_InventoryScanSession_RoomId " +
                    "ON InventoryScanSession(RoomId)");

                await _connection.ExecuteAsync(
                    "CREATE INDEX IF NOT EXISTS IX_InventoryScanSession_OfficeId " +
                    "ON InventoryScanSession(OfficeId)");

                // Seed default admin user
                await SeedDefaultAdminAsync();

                _isInitialized = true;
                return _connection;
            }
            finally
            {
                _initLock.Release();
            }
        }

        public async Task<bool> IsInitialSetupCompleteAsync()
        {
            var db = await GetConnectionAsync();
            var buildingCount = await db.Table<Building>().CountAsync();
            var locationCount = await db.Table<InventoryLocation>().CountAsync();
            var roomCount = await db.Table<Room>().CountAsync();
            return buildingCount > 0 && locationCount > 0 && roomCount > 0;
        }

        private async Task EnsureTableAsync<T>(string tableName) where T : new()
        {
            if (!await TableExistsAsync(tableName))
                await _connection!.CreateTableAsync<T>();
        }

        private async Task<bool> TableExistsAsync(string tableName)
        {
            var count = await _connection!.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM sqlite_master WHERE type = 'table' AND name = ?",
                tableName);
            return count > 0;
        }

        private async Task EnsureColumnAsync(string tableName, string columnName, string columnDefinition)
        {
            if (!await TableExistsAsync(tableName))
                return;

            var exists = await _connection!.ExecuteScalarAsync<int>(
                $"SELECT COUNT(1) FROM pragma_table_info('{tableName}') WHERE name = ?",
                columnName);

            if (exists == 0)
                await _connection.ExecuteAsync($"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition}");
        }

        private async Task EnsureScannedItemTableAsync()
        {
            if (!await TableExistsAsync("ScannedItem"))
            {
                await CreateScannedItemTableAsync();
                return;
            }

            var hasBuildingId = await ColumnExistsAsync("ScannedItem", "BuildingId");
            var hasLocationId = await ColumnExistsAsync("ScannedItem", "LocationId");
            var hasRoomId = await ColumnExistsAsync("ScannedItem", "RoomId");
            var hasOfficeId = await ColumnExistsAsync("ScannedItem", "OfficeId");
            var hasForeignKey = await HasForeignKeyAsync("ScannedItem", "InventoryScanSession");

            if (!hasBuildingId && !hasLocationId && !hasRoomId && !hasOfficeId && hasForeignKey)
                return;

            await RebuildScannedItemTableAsync();
        }

        private async Task CreateScannedItemTableAsync(string tableName = "ScannedItem")
        {
            await _connection!.ExecuteAsync($@"
                CREATE TABLE IF NOT EXISTS {tableName} (
                    ScanId INTEGER PRIMARY KEY AUTOINCREMENT,
                    SessionId INTEGER NOT NULL,
                    UniqueIdentifier VARCHAR(500) NOT NULL,
                    BarcodeType VARCHAR(50),
                    ScannedAt DATETIME NOT NULL,
                    Remarks VARCHAR(500),
                    Status VARCHAR(50),
                    FOREIGN KEY(SessionId) REFERENCES InventoryScanSession(SessionId) ON DELETE CASCADE
                )");
        }

        private async Task RebuildScannedItemTableAsync()
        {
            await _connection!.ExecuteAsync("PRAGMA foreign_keys = OFF");
            try
            {
                await _connection.ExecuteAsync("DROP TABLE IF EXISTS ScannedItem_New");
                await CreateScannedItemTableAsync("ScannedItem_New");
                await _connection.ExecuteAsync(@"
                    INSERT INTO ScannedItem_New
                        (ScanId, SessionId, UniqueIdentifier, BarcodeType, ScannedAt, Remarks, Status)
                    SELECT
                        ScanId,
                        SessionId,
                        UniqueIdentifier,
                        BarcodeType,
                        ScannedAt,
                        Remarks,
                        Status
                    FROM ScannedItem
                    WHERE EXISTS (
                        SELECT 1
                        FROM InventoryScanSession
                        WHERE InventoryScanSession.SessionId = ScannedItem.SessionId
                    )");
                await _connection.ExecuteAsync("DROP TABLE ScannedItem");
                await _connection.ExecuteAsync("ALTER TABLE ScannedItem_New RENAME TO ScannedItem");
            }
            finally
            {
                await _connection.ExecuteAsync("PRAGMA foreign_keys = ON");
            }
        }

        private async Task<bool> ColumnExistsAsync(string tableName, string columnName)
        {
            var exists = await _connection!.ExecuteScalarAsync<int>(
                $"SELECT COUNT(1) FROM pragma_table_info('{tableName}') WHERE name = ?",
                columnName);
            return exists > 0;
        }

        private async Task<bool> HasForeignKeyAsync(string tableName, string referencedTable)
        {
            var count = await _connection!.ExecuteScalarAsync<int>(
                $"SELECT COUNT(1) FROM pragma_foreign_key_list('{tableName}') WHERE [table] = ?",
                referencedTable);
            return count > 0;
        }

        /// <summary>
        /// Seeds the default admin user if no users exist.
        /// </summary>
        private async Task SeedDefaultAdminAsync()
        {
            var userCount = await _connection!.Table<User>().CountAsync();
            if (userCount == 0)
            {
                string salt = SecurityHelper.GenerateSalt();
                string hash = SecurityHelper.HashPassword(Constants.DefaultAdminPassword, salt);

                var admin = new User
                {
                    Username = Constants.DefaultAdminUsername,
                    PasswordHash = hash,
                    Salt = salt,
                    DisplayName = Constants.DefaultAdminDisplayName,
                    CreatedDate = DateTime.Now
                };

                await _connection.InsertAsync(admin);
            }
        }

        /// <summary>
        /// Closes the database connection.
        /// </summary>
        public async Task CloseAsync()
        {
            if (_connection != null)
            {
                await _connection.CloseAsync();
                _connection = null;
                _isInitialized = false;
            }
        }
    }
}
