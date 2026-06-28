using System;
using System.Threading.Tasks;
using InventoryScanner.Models;
using InventoryScanner.Repositories.Interfaces;
using InventoryScanner.Services.Interfaces;

namespace InventoryScanner.Repositories
{
    /// <summary>
    /// SQLite implementation of the user repository.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly IDatabaseService _databaseService;

        public UserRepository(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<User>()
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<int> CreateAsync(User user)
        {
            var db = await _databaseService.GetConnectionAsync();
            user.CreatedDate = DateTime.Now;
            return await db.InsertAsync(user);
        }

        public async Task<int> UpdateAsync(User user)
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.UpdateAsync(user);
        }

        public async Task<bool> ExistsAsync(string username)
        {
            var db = await _databaseService.GetConnectionAsync();
            var count = await db.Table<User>()
                .CountAsync(u => u.Username == username);
            return count > 0;
        }
    }
}
