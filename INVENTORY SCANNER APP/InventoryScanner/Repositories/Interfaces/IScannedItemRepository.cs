using System.Collections.Generic;
using System.Threading.Tasks;
using InventoryScanner.Models;

namespace InventoryScanner.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for scanned item data access.
    /// </summary>
    public interface IScannedItemRepository
    {
        Task<List<ScannedItem>> GetBySessionIdAsync(int sessionId);
        Task<ScannedItem?> GetByIdAsync(int scanId);
        Task<bool> ExistsInSessionAsync(int sessionId, string uniqueIdentifier);
        Task<int> InsertAsync(ScannedItem item);
        Task<int> DeleteAsync(int scanId);
        Task<int> DeleteBySessionIdAsync(int sessionId);
        Task<List<ScannedItem>> SearchAsync(int sessionId, string query);
        Task<int> GetCountBySessionIdAsync(int sessionId);
        Task<Dictionary<int, int>> GetScanCountsBySessionAsync();
        Task<List<ScannedItem>> GetAllAsync();
    }
}
