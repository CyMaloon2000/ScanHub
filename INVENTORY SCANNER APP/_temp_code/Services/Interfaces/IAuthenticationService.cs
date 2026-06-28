using System.Threading.Tasks;
using InventoryScanner.Models;

namespace InventoryScanner.Services.Interfaces
{
    /// <summary>
    /// Interface for offline authentication operations.
    /// </summary>
    public interface IAuthenticationService
    {
        Task<(bool Success, string Message)> LoginAsync(string username, string password);
        Task LogoutAsync();
        Task<bool> IsAuthenticatedAsync();
        Task<string> GetCurrentUsernameAsync();
    }
}
