using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryScanner.Models;
using InventoryScanner.Services.Interfaces;

namespace InventoryScanner.ViewModels
{
    /// <summary>
    /// ViewModel for the session list page. Displays, searches, and manages sessions.
    /// </summary>
    public partial class SessionListViewModel : BaseViewModel
    {
        private readonly ISessionService _sessionService;
        private readonly IAuthenticationService _authService;
        private readonly IEventAggregator _eventAggregator;

        public SessionListViewModel(
            ISessionService sessionService,
            IAuthenticationService authService,
            IEventAggregator eventAggregator)
        {
            _sessionService = sessionService;
            _authService = authService;
            _eventAggregator = eventAggregator;
            Title = "Inventory Sessions";

            _eventAggregator.Subscribe<SessionUpdatedEvent>(OnSessionUpdated);
        }

        [ObservableProperty]
        private ObservableCollection<InventorySession> _sessions = new();

        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private string _selectedStatusFilter = "All";

        [ObservableProperty]
        private string _currentUser = string.Empty;

        [ObservableProperty]
        private bool _isEmpty;

        public string[] StatusFilters { get; } = ["All", .. SessionStatus.All];

        /// <summary>
        /// Loads all sessions from the database.
        /// </summary>
        [RelayCommand]
        private async Task LoadSessionsAsync()
        {
            await ExecuteBusyAsync(async () =>
            {
                CurrentUser = await _authService.GetCurrentUsernameAsync();

                List<InventorySession> sessions;

                if (!string.IsNullOrWhiteSpace(SearchQuery) || SelectedStatusFilter != "All")
                {
                    var statusFilter = SelectedStatusFilter == "All" ? null : SelectedStatusFilter;
                    sessions = await _sessionService.SearchSessionsAsync(
                        query: SearchQuery, status: statusFilter);
                }
                else
                {
                    sessions = await _sessionService.GetAllSessionsAsync();
                }

                Sessions.Clear();
                foreach (var session in sessions)
                {
                    Sessions.Add(session);
                }

                IsEmpty = Sessions.Count == 0;
            }, "Loading sessions");
        }

        /// <summary>
        /// Navigates to create a new session.
        /// </summary>
        [RelayCommand]
        private async Task CreateSessionAsync()
        {
            await NavigateToAsync("SessionDetail", new Dictionary<string, object>
            {
                { "IsNew", true }
            });
        }

        /// <summary>
        /// Navigates to view/edit a selected session.
        /// </summary>
        [RelayCommand]
        private async Task SelectSessionAsync(InventorySession? session)
        {
            if (session == null) return;

            await NavigateToAsync("SessionDetail", new Dictionary<string, object>
            {
                { "SessionId", session.SessionId },
                { "IsNew", false }
            });
        }

        /// <summary>
        /// Deletes a session with confirmation.
        /// </summary>
        [RelayCommand]
        private async Task DeleteSessionAsync(InventorySession? session)
        {
            if (session == null) return;

            var confirmed = await ShowConfirmAsync("Delete Session",
                $"Are you sure you want to delete '{session.SessionName}'?\nThis will also delete all scanned items in this session.",
                "Delete", "Cancel");

            if (!confirmed) return;

            var (success, message) = await _sessionService.DeleteSessionAsync(session.SessionId, cascadeDelete: true);

            if (success)
            {
                Sessions.Remove(session);
                IsEmpty = Sessions.Count == 0;
                StatusMessage = message;
            }
            else
            {
                await ShowAlertAsync("Error", message);
            }
        }

        /// <summary>
        /// Performs search with current filters.
        /// </summary>
        [RelayCommand]
        private async Task SearchAsync()
        {
            await LoadSessionsAsync();
        }

        /// <summary>
        /// Navigates to export/import page.
        /// </summary>
        [RelayCommand]
        private async Task OpenExportImportAsync()
        {
            await NavigateToAsync("//Sync");
        }

        /// <summary>
        /// Logs out the current user.
        /// </summary>
        [RelayCommand]
        private async Task LogoutAsync()
        {
            var confirmed = await ShowConfirmAsync("Logout", "Are you sure you want to log out?");
            if (!confirmed) return;

            await _authService.LogoutAsync();
            await Shell.Current.GoToAsync("//Login");
        }

        private void OnSessionUpdated(SessionUpdatedEvent evt)
        {
            MainThread.BeginInvokeOnMainThread(async () => await LoadSessionsAsync());
        }
    }
}
