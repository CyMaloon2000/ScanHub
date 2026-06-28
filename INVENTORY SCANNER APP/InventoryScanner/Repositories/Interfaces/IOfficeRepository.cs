using InventoryScanner.Models;

namespace InventoryScanner.Repositories.Interfaces
{
    public interface IOfficeRepository
    {
        Task<List<Office>> GetAllAsync();
        Task<Office?> GetByIdAsync(int officeId);
        Task<Office?> GetByNameAsync(string officeName);
        Task<int> CreateAsync(Office office);
        Task<int> UpdateAsync(Office office);
        Task<int> DeleteAllAsync();
    }
}
