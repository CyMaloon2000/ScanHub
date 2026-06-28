using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryScanner.Helpers;
using InventoryScanner.Models;
using InventoryScanner.Repositories.Interfaces;
using InventoryScanner.Services.Interfaces;

namespace InventoryScanner.Services
{
    /// <summary>
    /// Business logic for inventory scan sessions.
    /// </summary>
    public class SessionService : ISessionService
    {
        private readonly ISessionRepository _sessionRepository;
        private readonly IScannedItemRepository _scannedItemRepository;
        private readonly IBuildingRepository _buildingRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly IRoomRepository _roomRepository;
        private readonly IOfficeRepository _officeRepository;
        private readonly IEventAggregator _eventAggregator;

        public SessionService(
            ISessionRepository sessionRepository,
            IScannedItemRepository scannedItemRepository,
            IBuildingRepository buildingRepository,
            ILocationRepository locationRepository,
            IRoomRepository roomRepository,
            IOfficeRepository officeRepository,
            IEventAggregator eventAggregator)
        {
            _sessionRepository = sessionRepository;
            _scannedItemRepository = scannedItemRepository;
            _buildingRepository = buildingRepository;
            _locationRepository = locationRepository;
            _roomRepository = roomRepository;
            _officeRepository = officeRepository;
            _eventAggregator = eventAggregator;
        }

        public async Task<List<InventorySession>> GetAllSessionsAsync()
        {
            var sessions = await _sessionRepository.GetAllAsync();
            return await EnrichSessionsAsync(sessions);
        }

        public async Task<InventorySession?> GetSessionByIdAsync(int sessionId)
        {
            var session = await _sessionRepository.GetByIdAsync(sessionId);
            if (session == null)
                return null;

            var enriched = await EnrichSessionAsync(session);
            enriched.ScanCount = await _scannedItemRepository.GetCountBySessionIdAsync(sessionId);
            return enriched;
        }

        public async Task<List<InventorySession>> SearchSessionsAsync(string? query = null, string? status = null)
        {
            var sessions = await _sessionRepository.SearchAsync(query, status);
            return await EnrichSessionsAsync(sessions);
        }

        public async Task<(bool Success, string Message)> CreateSessionAsync(InventorySession session)
        {
            try
            {
                var (isValid, error) = ValidationHelper.ValidateSession(session.BuildingId, session.LocationId, session.RoomId);
                if (!isValid)
                    return (false, error);

                var building = await _buildingRepository.GetByIdAsync(session.BuildingId);
                var location = await _locationRepository.GetByIdAsync(session.LocationId);
                var room = await _roomRepository.GetByIdAsync(session.RoomId);
                var office = session.OfficeId.HasValue ? await _officeRepository.GetByIdAsync(session.OfficeId.Value) : null;
                if (building == null || location == null || room == null ||
                    location.BuildingId != session.BuildingId || room.LocationId != session.LocationId)
                    return (false, "Selected building and location do not match.");

                session.SessionName = BuildSessionName(building.BuildingName, location.LocationName, room.RoomName, office?.OfficeName);
                session.BuildingName = building.BuildingName;
                session.LocationName = location.LocationName;
                session.RoomName = room.RoomName;
                session.OfficeName = office?.OfficeName ?? string.Empty;
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

        public async Task<(bool Success, string Message)> UpdateSessionAsync(InventorySession session)
        {
            try
            {
                var existing = await _sessionRepository.GetByIdAsync(session.SessionId);
                if (existing == null)
                    return (false, "Session not found.");

                if (existing.Status == SessionStatus.Completed || existing.Status == SessionStatus.Archived)
                    return (false, "Cannot edit a completed or archived session.");

                var (isValid, error) = ValidationHelper.ValidateSession(session.BuildingId, session.LocationId, session.RoomId);
                if (!isValid)
                    return (false, error);

                var building = await _buildingRepository.GetByIdAsync(session.BuildingId);
                var location = await _locationRepository.GetByIdAsync(session.LocationId);
                var room = await _roomRepository.GetByIdAsync(session.RoomId);
                var office = session.OfficeId.HasValue ? await _officeRepository.GetByIdAsync(session.OfficeId.Value) : null;
                if (building == null || location == null || room == null ||
                    location.BuildingId != session.BuildingId || room.LocationId != session.LocationId)
                    return (false, "Selected building, location, and room do not match.");

                session.SessionName = BuildSessionName(building.BuildingName, location.LocationName, room.RoomName, office?.OfficeName);
                session.BuildingName = building.BuildingName;
                session.LocationName = location.LocationName;
                session.RoomName = room.RoomName;
                session.OfficeName = office?.OfficeName ?? string.Empty;
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

        private async Task<List<InventorySession>> EnrichSessionsAsync(List<InventorySession> sessions)
        {
            var scanCounts = await _scannedItemRepository.GetScanCountsBySessionAsync();
            foreach (var session in sessions)
            {
                session.ScanCount = scanCounts.TryGetValue(session.SessionId, out var count) ? count : 0;
                await EnrichSessionAsync(session);
            }

            return sessions.OrderByDescending(s => s.UpdatedDate).ToList();
        }

        private async Task<InventorySession> EnrichSessionAsync(InventorySession session)
        {
            session.BuildingName = (await _buildingRepository.GetByIdAsync(session.BuildingId))?.BuildingName ?? string.Empty;
            session.LocationName = (await _locationRepository.GetByIdAsync(session.LocationId))?.LocationName ?? string.Empty;
            session.RoomName = (await _roomRepository.GetByIdAsync(session.RoomId))?.RoomName ?? string.Empty;
            session.OfficeName = session.OfficeId.HasValue
                ? (await _officeRepository.GetByIdAsync(session.OfficeId.Value))?.OfficeName ?? string.Empty
                : string.Empty;
            session.SessionName = string.IsNullOrWhiteSpace(session.SessionName)
                ? BuildSessionName(session.BuildingName, session.LocationName, session.RoomName, session.OfficeName)
                : session.SessionName;
            return session;
        }

        private static string BuildSessionName(string buildingName, string locationName, string roomName, string? officeName)
        {
            var officePart = string.IsNullOrWhiteSpace(officeName) ? string.Empty : $" / {officeName}";
            return $"{buildingName} - {locationName} / {roomName}{officePart}";
        }
    }
}
