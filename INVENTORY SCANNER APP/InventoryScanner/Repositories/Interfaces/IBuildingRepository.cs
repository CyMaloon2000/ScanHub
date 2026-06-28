using InventoryScanner.Models;

namespace InventoryScanner.Repositories.Interfaces
{
    public interface IBuildingRepository
    {
        Task<List<Building>> GetAllAsync();
        Task<Building?> GetByIdAsync(int buildingId);
        Task<Building?> GetByNameAsync(string buildingName);
        Task<int> CreateAsync(Building building);
        Task<int> UpdateAsync(Building building);
        Task<int> DeleteAllAsync();
    }
}
