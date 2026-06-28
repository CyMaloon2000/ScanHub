using InventoryScanner.Models;

namespace InventoryScanner.Services.Interfaces
{
    public interface IRoomService
    {
        Task<List<Room>> GetRoomsAsync(int locationId);
        Task<List<Room>> GetAllRoomsAsync();
        Task<(bool Success, string Message)> SaveRoomAsync(Room room);
    }
}
