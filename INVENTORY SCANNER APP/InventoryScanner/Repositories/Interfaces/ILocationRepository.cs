using InventoryLocation = InventoryScanner.Models.Location;

namespace InventoryScanner.Repositories.Interfaces
{
    public interface ILocationRepository
    {
        Task<List<InventoryLocation>> GetAllAsync();
        Task<List<InventoryLocation>> GetByBuildingIdAsync(int buildingId);
        Task<InventoryLocation?> GetByIdAsync(int locationId);
        Task<InventoryLocation?> GetByNameAndBuildingIdAsync(string locationName, int buildingId);
        Task<int> CreateAsync(InventoryLocation location);
        Task<int> UpdateAsync(InventoryLocation location);
        Task<int> DeleteAllAsync();
    }
}
