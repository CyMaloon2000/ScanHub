using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InventoryScanner.Helpers;
using InventoryScanner.Models;
using InventoryScanner.Repositories.Interfaces;
using InventoryScanner.Services.Interfaces;

namespace InventoryScanner.Services
{
    /// <summary>
    /// Business logic for inventory session management.
    /// </summary>
    public class SessionService : ISessionService
    {
        private readonly ISessionRepository _sessionRepository;
        private readonly IScannedItemRepository _scannedItemRepository;
        private readonly IEventAggregator _eventAggregator;

        public SessionService(
            ISessionRepository sessionRepository,
            IScannedItemRepository scannedItemRepository,
            IEventAggregator eventAggregator)
        {
            _sessionRepository = sessionRepository;
            _scannedItemRepository = scannedItemRepository;
            _eventAggregator = eventAggregator;
        }

        /// <inheritdoc/>
        public async Task<List<InventorySession>> GetAllSessionsAsync()
        {
            var sessions = await _sessionRepository.GetAllAsync();
            foreach (var session in sessions)
            {
                session.ScanCount = await _scannedItemRepository.GetCountBySessionIdAsync(session.SessionId);
            }
            return sessions;
        }

        /// <inheritdoc/>
        public async Task<InventorySession?> GetSessionByIdAsync(int sessionId)
        {
            var session = await _sessionRepository.GetByIdAsync(sessionId);
            if (session != null)
            {
                session.ScanCount = await _scannedItemRepository.GetCountBySessionIdAsync(sessionId);
            }
            return session;
        }

        /// <inheritdoc/>
        public async Task<List<InventorySession>> SearchSessionsAsync(string? school = null,
            string? campus = null, string? room = null, string? status = null, string? query = null)
        {
            var sessions = await _sessionRepository.SearchAsync(school, campus, room, status, query);
            foreach (var session in sessions)
            {
                session.ScanCount = await _scannedItemRepository.GetCountBySessionIdAsync(session.SessionId);
            }
            return sessions;
        }

        /// <inheritdoc/>
        public async Task<(bool Success, string Message)> CreateSessionAsync(InventorySession session)
        {
            try
            {
                var (isValid, error) = ValidationHelper.ValidateSession(
                    session.School, session.Campus, session.Location, session.Room);
                if (!isValid)
                    return (false, error);

                session.Status = SessionStatus.Draft;
                session.CreatedDate = DateTime.Now;
                session.UpdatedDate = DateTime.Now;

                await _sessionRepository.CreateAsync(session);

                _eventAggregator.Publish(new SessionUpdatedEvent
                {
                    Session = session,
                    Action = "Created"
                });

                return (true, "Session created successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Failed to create session: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<(bool Success, string Message)> UpdateSessionAsync(InventorySession session)
        {
            try
            {
                var existing = await _sessionRepository.GetByIdAsync(session.SessionId);
                if (existing == null)
                    return (false, "Session not found.");

                if (existing.Status == SessionStatus.Completed || existing.Status == SessionStatus.Archived)
                    return (false, "Cannot edit a completed or archived session.");

                var (isValid, error) = ValidationHelper.ValidateSession(
                    session.School, session.Campus, session.Location, session.Room);
                if (!isValid)
                    return (false, error);

                session.UpdatedDate = DateTime.Now;
                await _sessionRepository.UpdateAsync(session);

                _eventAggregator.Publish(new SessionUpdatedEvent
                {
                    Session = session,
                    Action = "Updated"
                });

                return (true, "Session updated successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Failed to update session: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<(bool Success, string Message)> DeleteSessionAsync(int sessionId, bool cascadeDelete = true)
        {
            try
            {
                var session = await _sessionRepository.GetByIdAsync(sessionId);
                if (session == null)
                    return (false, "Session not found.");

                if (cascadeDelete)
                {
                    await _scannedItemRepository.DeleteBySessionIdAsync(sessionId);
                }
                else
                {
                    var count = await _scannedItemRepository.GetCountBySessionIdAsync(sessionId);
                    if (count > 0)
                        return (false, $"Session has {count} scanned items. Delete them first or use cascade delete.");
                }

                await _sessionRepository.DeleteAsync(sessionId);

                _eventAggregator.Publish(new SessionUpdatedEvent
                {
                    Session = session,
                    Action = "Deleted"
                });

                return (true, "Session deleted successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Failed to delete session: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<(bool Success, string Message)> UpdateStatusAsync(int sessionId, string newStatus)
        {
            try
            {
                var session = await _sessionRepository.GetByIdAsync(sessionId);
                if (session == null)
                    return (false, "Session not found.");

                if (!SessionStatus.IsValidTransition(session.Status, newStatus))
                    return (false, $"Cannot change status from '{session.Status}' to '{newStatus}'.");

                session.Status = newStatus;
                session.UpdatedDate = DateTime.Now;
                await _sessionRepository.UpdateAsync(session);

                _eventAggregator.Publish(new SessionUpdatedEvent
                {
                    Session = session,
                    Action = "StatusChanged"
                });

                return (true, $"Status changed to '{newStatus}'.");
            }
            catch (Exception ex)
            {
                return (false, $"Failed to update status: {ex.Message}");
            }
        }
    }
}
