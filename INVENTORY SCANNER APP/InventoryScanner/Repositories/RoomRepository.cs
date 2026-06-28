using InventoryScanner.Models;
using InventoryScanner.Repositories.Interfaces;
using InventoryScanner.Services.Interfaces;

namespace InventoryScanner.Repositories
{
    public class RoomRepository : IRoomRepository
    {
        private readonly IDatabaseService _databaseService;

        public RoomRepository(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<List<Room>> GetAllAsync()
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<Room>().OrderBy(r => r.RoomName).ToListAsync();
        }

        public async Task<List<Room>> GetByLocationIdAsync(int locationId)
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<Room>()
                .Where(r => r.LocationId == locationId)
                .OrderBy(r => r.RoomName)
                .ToListAsync();
        }

        public async Task<Room?> GetByIdAsync(int roomId)
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<Room>().FirstOrDefaultAsync(r => r.RoomId == roomId);
        }

        public async Task<Room?> GetByNameAndLocationIdAsync(string roomName, int locationId)
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<Room>().FirstOrDefaultAsync(r =>
                r.LocationId == locationId &&
                r.RoomName.ToLower() == roomName.Trim().ToLower());
        }

        public async Task<int> CreateAsync(Room room)
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.InsertAsync(room);
        }

        public async Task<int> UpdateAsync(Room room)
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.UpdateAsync(room);
        }

        public async Task<int> DeleteAllAsync()
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.DeleteAllAsync<Room>();
        }
    }
}
