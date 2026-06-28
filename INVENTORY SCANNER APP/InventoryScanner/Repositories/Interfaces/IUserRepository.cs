using System.Threading.Tasks;
using InventoryScanner.Models;

namespace InventoryScanner.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for user data access.
    /// </summary>
    public interface IUserRepository
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<int> CreateAsync(User user);
        Task<int> UpdateAsync(User user);
        Task<bool> ExistsAsync(string username);
    }
}
