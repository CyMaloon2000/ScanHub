using InventoryScanner.Models;
using InventoryScanner.Repositories.Interfaces;
using InventoryScanner.Services.Interfaces;

namespace InventoryScanner.Repositories
{
    public class OfficeRepository : IOfficeRepository
    {
        private readonly IDatabaseService _databaseService;

        public OfficeRepository(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<List<Office>> GetAllAsync()
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<Office>().OrderBy(o => o.OfficeName).ToListAsync();
        }

        public async Task<Office?> GetByIdAsync(int officeId)
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<Office>().FirstOrDefaultAsync(o => o.OfficeId == officeId);
        }

        public async Task<Office?> GetByNameAsync(string officeName)
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<Office>().FirstOrDefaultAsync(o =>
                o.OfficeName.ToLower() == officeName.Trim().ToLower());
        }

        public async Task<int> CreateAsync(Office office)
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.InsertAsync(office);
        }

        public async Task<int> UpdateAsync(Office office)
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.UpdateAsync(office);
        }

        public async Task<int> DeleteAllAsync()
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.DeleteAllAsync<Office>();
        }
    }
}
