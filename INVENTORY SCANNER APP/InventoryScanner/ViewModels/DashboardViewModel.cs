using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System;
using System.Linq;
using System.Threading.Tasks;
using InventoryScanner.Models;
using InventoryScanner.Services.Interfaces;

namespace InventoryScanner.ViewModels
{
    /// <summary>
    /// ViewModel for the main dashboard view. Computes overall statistics across all rooms/sessions.
    /// </summary>
    public partial class DashboardViewModel : BaseViewModel
    {
        private readonly ISessionService _sessionService;

        public DashboardViewModel(ISessionService sessionService)
        {
            _sessionService = sessionService;
            Title = "Dashboard";
        }

        [ObservableProperty]
        private int _totalSessions;

        [ObservableProperty]
        private int _totalScannedItems;

        [ObservableProperty]
        private bool _hasSessions;

        [ObservableProperty]
        private int _draftSessions;

        [ObservableProperty]
        private int _inProgressSessions;

        [ObservableProperty]
        private int _completedSessions;

        [ObservableProperty]
        private int _archivedSessions;

        [ObservableProperty]
        private string _lastUpdatedSummary = "No activity yet";

        [ObservableProperty]
        private ObservableCollection<InventorySession> _recentSessions = new();

        /// <summary>
        /// Loads summary data from the SQLite database.
        /// </summary>
        [RelayCommand]
        private async Task LoadSummaryAsync()
        {
            await ExecuteBusyAsync(async () =>
            {
                var sessions = await _sessionService.GetAllSessionsAsync();
                
                TotalSessions = sessions.Count;
                HasSessions = sessions.Count > 0;
                
                if (HasSessions)
                {
                    TotalScannedItems = sessions.Sum(s => s.ScanCount);
                    DraftSessions = sessions.Count(s => s.Status == SessionStatus.Draft);
                    InProgressSessions = sessions.Count(s => s.Status == SessionStatus.InProgress);
                    CompletedSessions = sessions.Count(s => s.Status == SessionStatus.Completed);
                    ArchivedSessions = sessions.Count(s => s.Status == SessionStatus.Archived);
                    LastUpdatedSummary = $"Last update: {sessions.Max(s => s.UpdatedDate):MMM d, yyyy h:mm tt}";

                    RecentSessions.Clear();
                    foreach (var session in sessions.OrderByDescending(s => s.UpdatedDate).Take(5))
                    {
                        RecentSessions.Add(session);
                    }
                }
                else
                {
                    TotalScannedItems = 0;
                    DraftSessions = 0;
                    InProgressSessions = 0;
                    CompletedSessions = 0;
                    ArchivedSessions = 0;
                    LastUpdatedSummary = "No activity yet";
                    RecentSessions.Clear();
                }
            }, "Loading dashboard summary");
        }
    }
}
