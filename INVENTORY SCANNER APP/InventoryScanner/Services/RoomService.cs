using InventoryScanner.Models;
using InventoryScanner.Repositories.Interfaces;
using InventoryScanner.Services.Interfaces;

namespace InventoryScanner.Services
{
    public class RoomService : IRoomService
    {
        private readonly IRoomRepository _roomRepository;
        private readonly ILocationRepository _locationRepository;

        public RoomService(IRoomRepository roomRepository, ILocationRepository locationRepository)
        {
            _roomRepository = roomRepository;
            _locationRepository = locationRepository;
        }

        public Task<List<Room>> GetRoomsAsync(int locationId) => _roomRepository.GetByLocationIdAsync(locationId);

        public Task<List<Room>> GetAllRoomsAsync() => _roomRepository.GetAllAsync();

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
                await _roomRepository.UpdateAsync(existing);
                return (true, "Room updated.");
            }

            room.RoomName = room.RoomName.Trim();
            await _roomRepository.CreateAsync(room);
            return (true, "Room added.");
        }
    }
}
