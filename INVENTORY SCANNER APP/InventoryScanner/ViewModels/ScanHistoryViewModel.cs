using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryScanner.Models;
using InventoryScanner.Repositories.Interfaces;
using InventoryScanner.Services.Interfaces;

namespace InventoryScanner.ViewModels
{
    /// <summary>
    /// ViewModel for viewing scan history of a specific session.
    /// </summary>
    [QueryProperty(nameof(SessionId), "SessionId")]
    public partial class ScanHistoryViewModel : BaseViewModel
    {
        private readonly IScannedItemRepository _scannedItemRepository;
        private readonly ISessionService _sessionService;

        public ScanHistoryViewModel(
            IScannedItemRepository scannedItemRepository,
            ISessionService sessionService)
        {
            _scannedItemRepository = scannedItemRepository;
            _sessionService = sessionService;
            Title = "Scan History";
        }

        [ObservableProperty]
        private int _sessionId;

        [ObservableProperty]
        private string _sessionName = string.Empty;

        [ObservableProperty]
        private ObservableCollection<ScannedItem> _scannedItems = new();

        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private int _totalCount;

        [ObservableProperty]
        private bool _isEmpty;

        /// <summary>
        /// Loads scan history for the current session.
        /// </summary>
        [RelayCommand]
        private async Task LoadHistoryAsync()
        {
            await ExecuteBusyAsync(async () =>
            {
                var session = await _sessionService.GetSessionByIdAsync(SessionId);
                if (session != null)
                {
                    SessionName = session.DisplayName;
                    Title = $"History: {session.DisplayName}";
                }

                List<ScannedItem> items;

                if (!string.IsNullOrWhiteSpace(SearchQuery))
                {
                    items = await _scannedItemRepository.SearchAsync(SessionId, SearchQuery);
                }
                else
                {
                    items = await _scannedItemRepository.GetBySessionIdAsync(SessionId);
                }

                ScannedItems.Clear();
                foreach (var item in items)
                {
                    ScannedItems.Add(item);
                }

                TotalCount = ScannedItems.Count;
                IsEmpty = TotalCount == 0;
            }, "Loading history");
        }

        /// <summary>
        /// Searches scan history.
        /// </summary>
        [RelayCommand]
        private async Task SearchAsync()
        {
            await LoadHistoryAsync();
        }

        /// <summary>
        /// Deletes a single scanned item with confirmation.
        /// </summary>
        [RelayCommand]
        private async Task DeleteItemAsync(ScannedItem? item)
        {
            if (item == null) return;

            var confirmed = await ShowConfirmAsync("Delete Item",
                $"Remove '{item.UniqueIdentifier}' from scan history?",
                "Delete", "Cancel");

            if (!confirmed) return;

            await _scannedItemRepository.DeleteAsync(item.ScanId);
            ScannedItems.Remove(item);
            TotalCount = ScannedItems.Count;
            IsEmpty = TotalCount == 0;
        }

        /// <summary>
        /// Clears all scan history for this session with confirmation.
        /// </summary>
        [RelayCommand]
        private async Task ClearAllAsync()
        {
            if (ScannedItems.Count == 0) return;

            var confirmed = await ShowConfirmAsync("Clear All",
                $"Delete all {TotalCount} scanned items from this session?\nThis action cannot be undone.",
                "Clear All", "Cancel");

            if (!confirmed) return;

            await _scannedItemRepository.DeleteBySessionIdAsync(SessionId);
            ScannedItems.Clear();
            TotalCount = 0;
            IsEmpty = true;
            StatusMessage = "All items cleared.";
        }

        /// <summary>
        /// Navigates back to scanner.
        /// </summary>
        [RelayCommand]
        private async Task GoBackAsync()
        {
            await NavigateBackAsync();
        }
    }
}
