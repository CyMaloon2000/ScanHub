using InventoryLocation = InventoryScanner.Models.Location;
using InventoryScanner.Repositories.Interfaces;
using InventoryScanner.Services.Interfaces;

namespace InventoryScanner.Repositories
{
    public class LocationRepository : ILocationRepository
    {
        private readonly IDatabaseService _databaseService;

        public LocationRepository(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<List<InventoryLocation>> GetAllAsync()
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<InventoryLocation>().OrderBy(l => l.LocationName).ToListAsync();
        }

        public async Task<List<InventoryLocation>> GetByBuildingIdAsync(int buildingId)
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<InventoryLocation>()
                .Where(l => l.BuildingId == buildingId)
                .OrderBy(l => l.LocationName)
                .ToListAsync();
        }

        public async Task<InventoryLocation?> GetByIdAsync(int locationId)
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<InventoryLocation>().FirstOrDefaultAsync(l => l.LocationId == locationId);
        }

        public async Task<InventoryLocation?> GetByNameAndBuildingIdAsync(string locationName, int buildingId)
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<InventoryLocation>().FirstOrDefaultAsync(l =>
                l.BuildingId == buildingId &&
                l.LocationName.ToLower() == locationName.Trim().ToLower());
        }

        public async Task<int> CreateAsync(InventoryLocation location)
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.InsertAsync(location);
        }

        public async Task<int> UpdateAsync(InventoryLocation location)
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.UpdateAsync(location);
        }

        public async Task<int> DeleteAllAsync()
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.DeleteAllAsync<InventoryLocation>();
        }
    }
}
