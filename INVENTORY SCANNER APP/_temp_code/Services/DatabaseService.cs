using System;
using System.IO;
using System.Threading.Tasks;
using SQLite;
using InventoryScanner.Models;
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

                // Create tables
                await _connection.CreateTableAsync<InventorySession>();
                await _connection.CreateTableAsync<ScannedItem>();
                await _connection.CreateTableAsync<User>();

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
