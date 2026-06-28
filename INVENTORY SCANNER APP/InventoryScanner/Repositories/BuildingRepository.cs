using InventoryScanner.Models;
using InventoryScanner.Repositories.Interfaces;
using InventoryScanner.Services.Interfaces;

namespace InventoryScanner.Repositories
{
    public class BuildingRepository : IBuildingRepository
    {
        private readonly IDatabaseService _databaseService;

        public BuildingRepository(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<List<Building>> GetAllAsync()
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<Building>().OrderBy(b => b.BuildingName).ToListAsync();
        }

        public async Task<Building?> GetByIdAsync(int buildingId)
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<Building>().FirstOrDefaultAsync(b => b.BuildingId == buildingId);
        }

        public async Task<Building?> GetByNameAsync(string buildingName)
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<Building>().FirstOrDefaultAsync(b =>
                b.BuildingName.ToLower() == buildingName.Trim().ToLower());
        }

        public async Task<int> CreateAsync(Building building)
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.InsertAsync(building);
        }

        public async Task<int> UpdateAsync(Building building)
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.UpdateAsync(building);
        }

        public async Task<int> DeleteAllAsync()
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.DeleteAllAsync<Building>();
        }
    }
}
