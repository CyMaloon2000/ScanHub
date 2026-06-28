using System.Text.Json;
using InventoryScanner.Helpers;
using InventoryLocation = InventoryScanner.Models.Location;
using InventoryScanner.Models;
using InventoryScanner.Repositories.Interfaces;
using InventoryScanner.Services.Interfaces;

namespace InventoryScanner.Services
{
    public class SetupService : ISetupService
    {
        private readonly IDatabaseService _databaseService;
        private readonly IBuildingRepository _buildingRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly IRoomRepository _roomRepository;
        private readonly IOfficeRepository _officeRepository;
        private readonly IEventAggregator _eventAggregator;

        public SetupService(
            IDatabaseService databaseService,
            IBuildingRepository buildingRepository,
            ILocationRepository locationRepository,
            IRoomRepository roomRepository,
            IOfficeRepository officeRepository,
            IEventAggregator eventAggregator)
        {
            _databaseService = databaseService;
            _buildingRepository = buildingRepository;
            _locationRepository = locationRepository;
            _roomRepository = roomRepository;
            _officeRepository = officeRepository;
            _eventAggregator = eventAggregator;
        }

        public Task<bool> IsSetupCompleteAsync() => _databaseService.IsInitialSetupCompleteAsync();

        public Task<List<Building>> GetBuildingsAsync() => _buildingRepository.GetAllAsync();

        public Task<List<InventoryLocation>> GetLocationsAsync(int buildingId) => _locationRepository.GetByBuildingIdAsync(buildingId);

        public Task<List<Room>> GetRoomsAsync(int locationId) => _roomRepository.GetByLocationIdAsync(locationId);

        public Task<List<Office>> GetOfficesAsync() => _officeRepository.GetAllAsync();

        public async Task<(bool Success, string Message)> SaveBuildingAsync(Building building)
        {
            if (string.IsNullOrWhiteSpace(building.BuildingName))
                return (false, "Building name is required.");

            var existing = await _buildingRepository.GetByNameAsync(building.BuildingName);
            if (existing != null)
            {
                existing.BuildingName = building.BuildingName.Trim();
                await _buildingRepository.UpdateAsync(existing);
                return (true, "Building updated.");
            }

            building.BuildingName = building.BuildingName.Trim();
            await _buildingRepository.CreateAsync(building);
            return (true, "Building added.");
        }

        public async Task<(bool Success, string Message)> SaveLocationAsync(InventoryLocation location)
        {
            if (string.IsNullOrWhiteSpace(location.LocationName))
                return (false, "Location name is required.");

            if (location.BuildingId <= 0)
                return (false, "Building is required.");

            var existing = await _locationRepository.GetByNameAndBuildingIdAsync(location.LocationName, location.BuildingId);
            if (existing != null)
            {
                existing.LocationName = location.LocationName.Trim();
                existing.BuildingId = location.BuildingId;
                await _locationRepository.UpdateAsync(existing);
                return (true, "Location updated.");
            }

            location.LocationName = location.LocationName.Trim();
            await _locationRepository.CreateAsync(location);
            return (true, "Location added.");
        }

        public async Task<(bool Success, string Message)> SaveRoomAsync(Room room)
        {
            if (string.IsNullOrWhiteSpace(room.RoomName))
                return (false, "Room name is required.");

            if (room.LocationId <= 0)
                return (false, "Location is required.");

            var location = await _locationRepository.GetByIdAsync(room.LocationId);
            if (location == null)
                return (false, "Selected location does not exist.");

            if (room.RoomId > 0)
            {
                var current = await _roomRepository.GetByIdAsync(room.RoomId);
                if (current == null)
                    return (false, "Selected room does not exist.");

                current.RoomName = room.RoomName.Trim();
                current.LocationId = room.LocationId;
                await _roomRepository.UpdateAsync(current);
                return (true, "Room updated.");
            }

            var existing = await _roomRepository.GetByNameAndLocationIdAsync(room.RoomName, room.LocationId);
            if (existing != null)
            {
                existing.RoomName = room.RoomName.Trim();
                existing.LocationId = room.LocationId;
                await _roomRepository.UpdateAsync(existing);
                return (true, "Room updated.");
            }

            room.RoomName = room.RoomName.Trim();
            await _roomRepository.CreateAsync(room);
            return (true, "Room added.");
        }

        public async Task<(bool Success, string Message)> SaveOfficeAsync(Office office)
        {
            if (string.IsNullOrWhiteSpace(office.OfficeName))
                return (false, "Office name is required.");

            var existing = await _officeRepository.GetByNameAsync(office.OfficeName);
            if (existing != null)
            {
                existing.OfficeName = office.OfficeName.Trim();
                await _officeRepository.UpdateAsync(existing);
                return (true, "Office updated.");
            }

            office.OfficeName = office.OfficeName.Trim();
            await _officeRepository.CreateAsync(office);
            return (true, "Office added.");
        }

        public async Task<(bool Success, string Message)> ClearAllDataAsync()
        {
            try
            {
                var db = await _databaseService.GetConnectionAsync();

                await db.ExecuteAsync("PRAGMA foreign_keys = OFF");
                try
                {
                    await db.ExecuteAsync("DELETE FROM ScannedItem");
                    await db.ExecuteAsync("DELETE FROM InventoryScanSession");
                    await db.ExecuteAsync("DELETE FROM Room");
                    await db.ExecuteAsync("DELETE FROM Location");
                    await db.ExecuteAsync("DELETE FROM Building");
                    await db.ExecuteAsync("DELETE FROM Office");
                    await db.ExecuteAsync("DELETE FROM User");

                    await db.ExecuteAsync(
                        "DELETE FROM sqlite_sequence WHERE name IN ('ScannedItem', 'InventoryScanSession', 'Room', 'Location', 'Building', 'Office', 'User')");
                }
                finally
                {
                    await db.ExecuteAsync("PRAGMA foreign_keys = ON");
                }

                var salt = SecurityHelper.GenerateSalt();
                await db.InsertAsync(new User
                {
                    Username = Constants.DefaultAdminUsername,
                    PasswordHash = SecurityHelper.HashPassword(Constants.DefaultAdminPassword, salt),
                    Salt = salt,
                    DisplayName = Constants.DefaultAdminDisplayName,
                    CreatedDate = DateTime.Now
                });

                return (true, "All local database data was deleted. Default admin account was recreated.");
            }
            catch (Exception ex)
            {
                return (false, $"Database reset failed: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message, int BuildingCount, int LocationCount, int RoomCount, int OfficeCount)> ImportSetupAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                    return (false, "Setup file not found.", 0, 0, 0, 0);

                var json = await File.ReadAllTextAsync(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var payload = JsonSerializer.Deserialize<SetupImportData>(json, options);
                if (payload == null)
                    return (false, "Invalid setup file format.", 0, 0, 0, 0);

                var validBuildings = payload.Buildings
                    .Where(b => b.BuildingId > 0 && !string.IsNullOrWhiteSpace(b.BuildingName))
                    .ToList();
                var incomingBuildingIds = validBuildings.Select(b => b.BuildingId).ToHashSet();
                var validLocations = payload.Locations
                    .Where(l => l.LocationId > 0 &&
                                l.BuildingId.HasValue &&
                                incomingBuildingIds.Contains(l.BuildingId.Value) &&
                                !string.IsNullOrWhiteSpace(l.LocationName))
                    .ToList();
                var incomingLocationIds = validLocations.Select(l => l.LocationId).ToHashSet();
                var validRooms = payload.Rooms
                    .Where(r => r.RoomId > 0 &&
                                r.LocationId.HasValue &&
                                incomingLocationIds.Contains(r.LocationId.Value) &&
                                !string.IsNullOrWhiteSpace(r.RoomName))
                    .ToList();
                var validOffices = payload.Offices
                    .Where(o => !string.IsNullOrWhiteSpace(o.OfficeName))
                    .ToList();

                if (validBuildings.Count == 0 || validLocations.Count == 0 || validRooms.Count == 0)
                    return (false, "Setup file must include Buildings, Locations, and Rooms.", 0, 0, 0, 0);

                var invalidLocations = payload.Locations
                    .Where(l => l.LocationId > 0 && l.BuildingId.HasValue)
                    .Any(l => !incomingBuildingIds.Contains(l.BuildingId!.Value));
                if (invalidLocations)
                    return (false, "Every location must reference a valid building.", 0, 0, 0, 0);

                var invalidRooms = payload.Rooms
                    .Where(r => r.RoomId > 0 && r.LocationId.HasValue && !string.IsNullOrWhiteSpace(r.RoomName))
                    .Any(r => !incomingLocationIds.Contains(r.LocationId!.Value));
                if (invalidRooms)
                    return (false, "Every room must reference a valid location.", 0, 0, 0, 0);

                await _roomRepository.DeleteAllAsync();
                await _locationRepository.DeleteAllAsync();
                await _buildingRepository.DeleteAllAsync();
                await _officeRepository.DeleteAllAsync();

                int buildingCount = 0;
                int locationCount = 0;
                int roomCount = 0;
                int officeCount = 0;
                var db = await _databaseService.GetConnectionAsync();

                foreach (var building in validBuildings)
                {
                    await db.ExecuteAsync(
                        "INSERT INTO Building (BuildingId, BuildingName) VALUES (?, ?)",
                        building.BuildingId,
                        building.BuildingName.Trim());
                    buildingCount++;
                }

                foreach (var location in validLocations)
                {
                    await db.ExecuteAsync(
                        "INSERT INTO Location (LocationId, LocationName, BuildingId) VALUES (?, ?, ?)",
                        location.LocationId,
                        location.LocationName.Trim(),
                        location.BuildingId!.Value);
                    locationCount++;
                }

                foreach (var room in validRooms)
                {
                    await db.ExecuteAsync(
                        "INSERT INTO Room (RoomId, RoomName, LocationId) VALUES (?, ?, ?)",
                        room.RoomId,
                        room.RoomName.Trim(),
                        room.LocationId!.Value);
                    roomCount++;
                }

                foreach (var office in validOffices)
                {
                    if (office.OfficeId > 0)
                    {
                        await db.ExecuteAsync(
                            "INSERT INTO Office (OfficeId, OfficeName) VALUES (?, ?)",
                            office.OfficeId,
                            office.OfficeName.Trim());
                    }
                    else
                    {
                        await _officeRepository.CreateAsync(new Office { OfficeName = office.OfficeName.Trim() });
                    }
                    officeCount++;
                }

                var verificationErrors = new List<string>();

                foreach (var building in validBuildings)
                {
                    var saved = await _buildingRepository.GetByIdAsync(building.BuildingId);
                    if (saved == null || !string.Equals(saved.BuildingName, building.BuildingName.Trim(), StringComparison.Ordinal))
                        verificationErrors.Add($"Building '{building.BuildingName}' was not saved correctly.");
                }

                foreach (var location in validLocations)
                {
                    var saved = await _locationRepository.GetByIdAsync(location.LocationId);
                    if (saved == null ||
                        saved.BuildingId != location.BuildingId ||
                        !string.Equals(saved.LocationName, location.LocationName.Trim(), StringComparison.Ordinal))
                    {
                        verificationErrors.Add($"Location '{location.LocationName}' was not saved correctly.");
                    }
                }

                foreach (var room in validRooms)
                {
                    var saved = await _roomRepository.GetByIdAsync(room.RoomId);
                    if (saved == null ||
                        saved.LocationId != room.LocationId ||
                        !string.Equals(saved.RoomName, room.RoomName.Trim(), StringComparison.Ordinal))
                    {
                        verificationErrors.Add($"Room '{room.RoomName}' was not saved correctly.");
                    }
                }

                foreach (var office in validOffices.Where(o => o.OfficeId > 0))
                {
                    var saved = await _officeRepository.GetByIdAsync(office.OfficeId);
                    if (saved == null || !string.Equals(saved.OfficeName, office.OfficeName.Trim(), StringComparison.Ordinal))
                        verificationErrors.Add($"Office '{office.OfficeName}' was not saved correctly.");
                }

                if (verificationErrors.Count > 0)
                {
                    return (false,
                        $"Setup import verification failed: {string.Join(" ", verificationErrors.Take(5))}",
                        buildingCount,
                        locationCount,
                        roomCount,
                        officeCount);
                }

                _eventAggregator.Publish(new SetupCompletedEvent
                {
                    BuildingCount = buildingCount,
                    LocationCount = locationCount,
                    RoomCount = roomCount,
                    OfficeCount = officeCount,
                    Message = $"Imported {buildingCount} building(s), {locationCount} location(s), {roomCount} room(s), and {officeCount} office(s)."
                });

                return (true, $"Imported {buildingCount} building(s), {locationCount} location(s), {roomCount} room(s), and {officeCount} office(s).",
                    buildingCount, locationCount, roomCount, officeCount);
            }
            catch (Exception ex)
            {
                return (false, $"Setup import failed: {ex.Message}", 0, 0, 0, 0);
            }
        }
    }
}
