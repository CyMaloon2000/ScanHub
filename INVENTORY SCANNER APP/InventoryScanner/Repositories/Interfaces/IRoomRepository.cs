using InventoryScanner.Models;

namespace InventoryScanner.Repositories.Interfaces
{
    public interface IRoomRepository
    {
        Task<List<Room>> GetAllAsync();
        Task<List<Room>> GetByLocationIdAsync(int locationId);
        Task<Room?> GetByIdAsync(int roomId);
        Task<Room?> GetByNameAndLocationIdAsync(string roomName, int locationId);
        Task<int> CreateAsync(Room room);
        Task<int> UpdateAsync(Room room);
        Task<int> DeleteAllAsync();
    }
}
