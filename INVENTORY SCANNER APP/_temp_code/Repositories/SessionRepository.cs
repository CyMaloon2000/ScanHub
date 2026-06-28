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
    /// SQLite implementation of the session repository.
    /// </summary>
    public class SessionRepository : ISessionRepository
    {
        private readonly IDatabaseService _databaseService;

        public SessionRepository(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<List<InventorySession>> GetAllAsync()
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<InventorySession>()
                .OrderByDescending(s => s.UpdatedDate)
                .ToListAsync();
        }

        public async Task<InventorySession?> GetByIdAsync(int sessionId)
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<InventorySession>()
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);
        }

        public async Task<List<InventorySession>> SearchAsync(string? school = null,
            string? campus = null, string? room = null, string? status = null, string? query = null)
        {
            var db = await _databaseService.GetConnectionAsync();
            var allSessions = await db.Table<InventorySession>().ToListAsync();

            var filtered = allSessions.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(school))
                filtered = filtered.Where(s => s.School.Contains(school, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(campus))
                filtered = filtered.Where(s => s.Campus.Contains(campus, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(room))
                filtered = filtered.Where(s => s.Room.Contains(room, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(status))
                filtered = filtered.Where(s => s.Status.Equals(status, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(query))
                filtered = filtered.Where(s =>
                    s.SessionName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    s.School.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    s.Campus.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    s.Location.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    s.Room.Contains(query, StringComparison.OrdinalIgnoreCase));

            return filtered.OrderByDescending(s => s.UpdatedDate).ToList();
        }

        public async Task<int> CreateAsync(InventorySession session)
        {
            var db = await _databaseService.GetConnectionAsync();
            session.CreatedDate = DateTime.Now;
            session.UpdatedDate = DateTime.Now;
            return await db.InsertAsync(session);
        }

        public async Task<int> UpdateAsync(InventorySession session)
        {
            var db = await _databaseService.GetConnectionAsync();
            session.UpdatedDate = DateTime.Now;
            return await db.UpdateAsync(session);
        }

        public async Task<int> DeleteAsync(int sessionId)
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.DeleteAsync<InventorySession>(sessionId);
        }

        public async Task<bool> ExistsAsync(int sessionId)
        {
            var db = await _databaseService.GetConnectionAsync();
            var count = await db.Table<InventorySession>()
                .CountAsync(s => s.SessionId == sessionId);
            return count > 0;
        }
    }
}
