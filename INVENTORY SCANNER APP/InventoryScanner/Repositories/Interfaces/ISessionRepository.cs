using System.Collections.Generic;
using System.Threading.Tasks;
using InventoryScanner.Models;

namespace InventoryScanner.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for inventory session data access.
    /// </summary>
    public interface ISessionRepository
    {
        Task<List<InventorySession>> GetAllAsync();
        Task<InventorySession?> GetByIdAsync(int sessionId);
        Task<List<InventorySession>> SearchAsync(string? query = null, string? status = null);
        Task<int> CreateAsync(InventorySession session);
        Task<int> UpdateAsync(InventorySession session);
        Task<int> DeleteAsync(int sessionId);
        Task<bool> ExistsAsync(int sessionId);
    }
}
