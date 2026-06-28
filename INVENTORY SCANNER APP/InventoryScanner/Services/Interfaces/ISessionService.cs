using System.Collections.Generic;
using System.Threading.Tasks;
using InventoryScanner.Models;

namespace InventoryScanner.Services.Interfaces
{
    /// <summary>
    /// Interface for inventory session business logic.
    /// </summary>
    public interface ISessionService
    {
        Task<List<InventorySession>> GetAllSessionsAsync();
        Task<InventorySession?> GetSessionByIdAsync(int sessionId);
        Task<List<InventorySession>> SearchSessionsAsync(string? query = null, string? status = null);
        Task<(bool Success, string Message)> CreateSessionAsync(InventorySession session);
        Task<(bool Success, string Message)> UpdateSessionAsync(InventorySession session);
        Task<(bool Success, string Message)> DeleteSessionAsync(int sessionId, bool cascadeDelete = true);
        Task<(bool Success, string Message)> UpdateStatusAsync(int sessionId, string newStatus);
    }
}
