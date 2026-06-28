using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryScanner.Models;
using InventoryScanner.Repositories.Interfaces;
using InventoryScanner.Services.Interfaces;

namespace InventoryScanner.Repositories
{
    /// <summary>
    /// SQLite implementation of the scanned item repository.
    /// </summary>
    public class ScannedItemRepository : IScannedItemRepository
    {
        private readonly IDatabaseService _databaseService;

        public ScannedItemRepository(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<List<ScannedItem>> GetBySessionIdAsync(int sessionId)
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<ScannedItem>()
                .Where(s => s.SessionId == sessionId)
                .OrderByDescending(s => s.ScannedAt)
                .ToListAsync();
        }

        public async Task<ScannedItem?> GetByIdAsync(int scanId)
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<ScannedItem>()
                .FirstOrDefaultAsync(s => s.ScanId == scanId);
        }

        public async Task<bool> ExistsInSessionAsync(int sessionId, string uniqueIdentifier)
        {
            var db = await _databaseService.GetConnectionAsync();
            var count = await db.Table<ScannedItem>()
                .CountAsync(s => s.SessionId == sessionId &&
                                 s.UniqueIdentifier == uniqueIdentifier);
            return count > 0;
        }

        public async Task<int> InsertAsync(ScannedItem item)
        {
            var db = await _databaseService.GetConnectionAsync();
            item.ScannedAt = DateTime.Now;
            return await db.InsertAsync(item);
        }

        public async Task<int> DeleteAsync(int scanId)
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.DeleteAsync<ScannedItem>(scanId);
        }

        public async Task<int> DeleteBySessionIdAsync(int sessionId)
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.ExecuteAsync(
                "DELETE FROM ScannedItem WHERE SessionId = ?", sessionId);
        }

        public async Task<List<ScannedItem>> SearchAsync(int sessionId, string query)
        {
            var db = await _databaseService.GetConnectionAsync();
            var items = await db.Table<ScannedItem>()
                .Where(s => s.SessionId == sessionId)
                .ToListAsync();

            if (string.IsNullOrWhiteSpace(query))
                return items.OrderByDescending(s => s.ScannedAt).ToList();

            return items.Where(s =>
                    s.UniqueIdentifier.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    s.BarcodeType.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(s.Remarks) && s.Remarks.Contains(query, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(s => s.ScannedAt)
                .ToList();
        }

        public async Task<int> GetCountBySessionIdAsync(int sessionId)
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<ScannedItem>()
                .CountAsync(s => s.SessionId == sessionId);
        }

        public async Task<List<ScannedItem>> GetAllAsync()
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<ScannedItem>().ToListAsync();
        }
    }
}
