using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using InventoryScanner.Helpers;
using InventoryScanner.Models;
using InventoryScanner.Repositories.Interfaces;
using InventoryScanner.Services.Export;
using InventoryScanner.Services.Interfaces;

namespace InventoryScanner.Services
{
    /// <summary>
    /// Handles exporting inventory session data using configurable format strategies.
    /// </summary>
    public class ExportService : IExportService
    {
        private readonly ISessionRepository _sessionRepository;
        private readonly IScannedItemRepository _scannedItemRepository;
        private readonly IBuildingRepository _buildingRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly IRoomRepository _roomRepository;
        private readonly IOfficeRepository _officeRepository;
        private readonly IEventAggregator _eventAggregator;

        public ExportService(
            ISessionRepository sessionRepository,
            IScannedItemRepository scannedItemRepository,
            IBuildingRepository buildingRepository,
            ILocationRepository locationRepository,
            IRoomRepository roomRepository,
            IOfficeRepository officeRepository,
            IEventAggregator eventAggregator)
        {
            _sessionRepository = sessionRepository;
            _scannedItemRepository = scannedItemRepository;
            _buildingRepository = buildingRepository;
            _locationRepository = locationRepository;
            _roomRepository = roomRepository;
            _officeRepository = officeRepository;
            _eventAggregator = eventAggregator;
        }

        /// <inheritdoc/>
        public async Task<(bool Success, string FilePath, string Message)> ExportSessionsAsync(
            List<int> sessionIds, string format)
        {
            try
            {
                var strategy = ExportStrategyFactory.Create(format);
                var exportData = new ExportData
                {
                    ExportDate = DateTime.Now,
                    ExportedBy = await GetCurrentUserAsync()
                };

                foreach (var sessionId in sessionIds)
                {
                    var session = await _sessionRepository.GetByIdAsync(sessionId);
                    if (session == null) continue;
                    await EnrichSessionAsync(session);

                    var items = await _scannedItemRepository.GetBySessionIdAsync(sessionId);
                    session.ScanCount = items.Count;

                    exportData.Sessions.Add(new SessionExportItem
                    {
                        Session = session,
                        Items = items
                    });
                }

                if (exportData.Sessions.Count == 0)
                    return (false, string.Empty, "No sessions found to export.");

                var outputDir = GetExportDirectory();
                var filePath = await strategy.ExportAsync(exportData, outputDir);

                _eventAggregator.Publish(new DataTransferEvent
                {
                    Operation = "Export",
                    Success = true,
                    FilePath = filePath,
                    Message = $"Exported {exportData.Sessions.Count} session(s) to {format}.",
                    RecordCount = exportData.Sessions.Sum(s => s.Items.Count)
                });

                return (true, filePath, $"Exported {exportData.Sessions.Count} session(s) successfully.");
            }
            catch (Exception ex)
            {
                return (false, string.Empty, $"Export failed: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<(bool Success, string FilePath, string Message)> ExportAllSessionsAsync(string format)
        {
            var sessions = await _sessionRepository.GetAllAsync();
            var sessionIds = sessions.Select(s => s.SessionId).ToList();
            return await ExportSessionsAsync(sessionIds, format);
        }

        /// <inheritdoc/>
        public string[] GetSupportedFormats() => ExportStrategyFactory.GetSupportedFormats();

        private string GetExportDirectory()
        {
            var dir = Path.Combine(FileSystem.AppDataDirectory, Constants.ExportFolderName);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }

        private async Task<string> GetCurrentUserAsync()
        {
            try
            {
                return await SecureStorage.Default.GetAsync(Constants.CurrentUserKey) ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        private async Task EnrichSessionAsync(InventorySession session)
        {
            session.BuildingName = (await _buildingRepository.GetByIdAsync(session.BuildingId))?.BuildingName ?? string.Empty;
            session.LocationName = (await _locationRepository.GetByIdAsync(session.LocationId))?.LocationName ?? string.Empty;
            session.RoomName = (await _roomRepository.GetByIdAsync(session.RoomId))?.RoomName ?? string.Empty;
            session.OfficeName = session.OfficeId.HasValue
                ? (await _officeRepository.GetByIdAsync(session.OfficeId.Value))?.OfficeName ?? string.Empty
                : string.Empty;
            if (string.IsNullOrWhiteSpace(session.SessionName))
            {
                var officePart = string.IsNullOrWhiteSpace(session.OfficeName) ? string.Empty : $" / {session.OfficeName}";
                session.SessionName = $"{session.BuildingName} - {session.LocationName} / {session.RoomName}{officePart}";
            }
        }
    }

    /// <summary>
    /// Handles importing inventory session data from JSON files.
    /// </summary>
    public class ImportService : IImportService
    {
        private readonly ISessionRepository _sessionRepository;
        private readonly IScannedItemRepository _scannedItemRepository;
        private readonly IBuildingRepository _buildingRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly IRoomRepository _roomRepository;
        private readonly IOfficeRepository _officeRepository;
        private readonly IEventAggregator _eventAggregator;

        public ImportService(
            ISessionRepository sessionRepository,
            IScannedItemRepository scannedItemRepository,
            IBuildingRepository buildingRepository,
            ILocationRepository locationRepository,
            IRoomRepository roomRepository,
            IOfficeRepository officeRepository,
            IEventAggregator eventAggregator)
        {
            _sessionRepository = sessionRepository;
            _scannedItemRepository = scannedItemRepository;
            _buildingRepository = buildingRepository;
            _locationRepository = locationRepository;
            _roomRepository = roomRepository;
            _officeRepository = officeRepository;
            _eventAggregator = eventAggregator;
        }

        /// <inheritdoc/>
        public async Task<(bool Success, string Message, int SessionCount, int ItemCount)> ImportAsync(string filePath)
        {
            try
            {
                var (isValid, validationMsg) = await ValidateImportFileAsync(filePath);
                if (!isValid)
                    return (false, validationMsg, 0, 0);

                var json = await File.ReadAllTextAsync(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                };

                var importData = JsonSerializer.Deserialize<ExportData>(json, options);
                if (importData?.Sessions == null || importData.Sessions.Count == 0)
                    return (false, "No sessions found in import file.", 0, 0);

                int sessionsImported = 0;
                int itemsImported = 0;

                foreach (var sessionItem in importData.Sessions)
                {
                    var session = sessionItem.Session;

                    var building = await ResolveBuildingAsync(session);
                    var location = await ResolveLocationAsync(session, building);
                    var room = await ResolveRoomAsync(session, location);
                    var office = await ResolveOfficeAsync(session);

                    if (building == null || location == null || room == null)
                        return (false, "Import file references catalog data that does not exist locally.", 0, 0);

                    // Create new session (reset ID to allow auto-increment)
                    var newSession = new InventorySession
                    {
                        SessionName = session.SessionName,
                        BuildingId = building.BuildingId,
                        LocationId = location.LocationId,
                        RoomId = room.RoomId,
                        OfficeId = office?.OfficeId,
                        Description = session.Description,
                        Status = session.Status,
                        CreatedBy = session.CreatedBy,
                        CreatedDate = session.CreatedDate,
                        ExpectedCount = session.ExpectedCount,
                        UpdatedDate = session.UpdatedDate
                    };

                    await _sessionRepository.CreateAsync(newSession);
                    sessionsImported++;

                    foreach (var item in sessionItem.Items)
                    {
                        // Check for duplicates within the new session
                        var exists = await _scannedItemRepository.ExistsInSessionAsync(
                            newSession.SessionId, item.UniqueIdentifier);

                        if (!exists)
                        {
                            var newItem = new ScannedItem
                            {
                                SessionId = newSession.SessionId,
                                UniqueIdentifier = item.UniqueIdentifier,
                                BarcodeType = item.BarcodeType,
                                ScannedAt = item.ScannedAt,
                                Remarks = item.Remarks,
                                Status = item.Status
                            };

                            try
                            {
                                await _scannedItemRepository.InsertAsync(newItem);
                                itemsImported++;
                            }
                            catch (SQLite.SQLiteException)
                            {
                                // Skip duplicates silently (DB constraint)
                            }
                        }
                    }
                }

                _eventAggregator.Publish(new DataTransferEvent
                {
                    Operation = "Import",
                    Success = true,
                    FilePath = filePath,
                    Message = $"Imported {sessionsImported} session(s) with {itemsImported} item(s).",
                    RecordCount = itemsImported
                });

                return (true, $"Successfully imported {sessionsImported} session(s) with {itemsImported} item(s).",
                    sessionsImported, itemsImported);
            }
            catch (Exception ex)
            {
                return (false, $"Import failed: {ex.Message}", 0, 0);
            }
        }

        /// <inheritdoc/>
        public async Task<(bool IsValid, string Message)> ValidateImportFileAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                    return (false, "File path is empty.");

                if (!File.Exists(filePath))
                    return (false, "File not found.");

                if (!filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    return (false, "Only JSON import files are supported.");

                var json = await File.ReadAllTextAsync(filePath);
                if (string.IsNullOrWhiteSpace(json))
                    return (false, "File is empty.");

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                };

                var data = JsonSerializer.Deserialize<ExportData>(json, options);
                if (data == null)
                    return (false, "Invalid file format.");

                if (data.Sessions == null || data.Sessions.Count == 0)
                    return (false, "No sessions found in file.");

                return (true, $"Valid file with {data.Sessions.Count} session(s).");
            }
            catch (JsonException)
            {
                return (false, "Invalid JSON format.");
            }
            catch (Exception ex)
            {
                return (false, $"Validation failed: {ex.Message}");
            }
        }

        private async Task<Building?> ResolveBuildingAsync(InventorySession session)
        {
            var building = await _buildingRepository.GetByIdAsync(session.BuildingId);
            if (building != null)
                return building;

            if (!string.IsNullOrWhiteSpace(session.BuildingName))
                return await _buildingRepository.GetByNameAsync(session.BuildingName);

            return null;
        }

        private async Task<InventoryScanner.Models.Location?> ResolveLocationAsync(InventorySession session, Building? building)
        {
            if (building == null)
                return null;

            var location = await _locationRepository.GetByIdAsync(session.LocationId);
            if (location != null && location.BuildingId == building.BuildingId)
                return location;

            if (!string.IsNullOrWhiteSpace(session.LocationName))
            {
                var candidates = await _locationRepository.GetByBuildingIdAsync(building.BuildingId);
                return candidates.FirstOrDefault(l =>
                    l.LocationName.Equals(session.LocationName, StringComparison.OrdinalIgnoreCase));
            }

            return null;
        }

        private async Task<Room?> ResolveRoomAsync(InventorySession session, InventoryScanner.Models.Location? location)
        {
            if (location == null)
                return null;

            var room = await _roomRepository.GetByIdAsync(session.RoomId);
            if (room != null && room.LocationId == location.LocationId)
                return room;

            if (!string.IsNullOrWhiteSpace(session.RoomName))
            {
                var candidates = await _roomRepository.GetByLocationIdAsync(location.LocationId);
                return candidates.FirstOrDefault(r =>
                    r.RoomName.Equals(session.RoomName, StringComparison.OrdinalIgnoreCase));
            }

            return null;
        }

        private async Task<Office?> ResolveOfficeAsync(InventorySession session)
        {
            if (!session.OfficeId.HasValue && string.IsNullOrWhiteSpace(session.OfficeName))
                return null;

            var office = session.OfficeId.HasValue ? await _officeRepository.GetByIdAsync(session.OfficeId.Value) : null;
            if (office != null)
                return office;

            if (!string.IsNullOrWhiteSpace(session.OfficeName))
                return await _officeRepository.GetByNameAsync(session.OfficeName);

            return null;
        }
    }
}
