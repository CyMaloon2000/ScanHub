using InventoryLocation = InventoryScanner.Models.Location;
using InventoryScanner.Models;

namespace InventoryScanner.Services.Interfaces
{
    public interface ISetupService
    {
        Task<bool> IsSetupCompleteAsync();
        Task<(bool Success, string Message, int BuildingCount, int LocationCount, int RoomCount, int OfficeCount)> ImportSetupAsync(string filePath);
        Task<List<Building>> GetBuildingsAsync();
        Task<List<InventoryLocation>> GetLocationsAsync(int buildingId);
        Task<List<Room>> GetRoomsAsync(int locationId);
        Task<List<Office>> GetOfficesAsync();
        Task<(bool Success, string Message)> SaveBuildingAsync(Building building);
        Task<(bool Success, string Message)> SaveLocationAsync(InventoryLocation location);
        Task<(bool Success, string Message)> SaveRoomAsync(Room room);
        Task<(bool Success, string Message)> SaveOfficeAsync(Office office);
        Task<(bool Success, string Message)> ClearAllDataAsync();
    }
}
