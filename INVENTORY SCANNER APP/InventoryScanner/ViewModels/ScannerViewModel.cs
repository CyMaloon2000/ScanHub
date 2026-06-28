using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryScanner.Models;
using InventoryScanner.Services.Interfaces;

namespace InventoryScanner.ViewModels
{
    [QueryProperty(nameof(SessionId), "SessionId")]
    public partial class ScannerViewModel : BaseViewModel
    {
        private readonly IScannerService _scannerService;
        private readonly ISessionService _sessionService;
        private readonly IEventAggregator _eventAggregator;
        private DateTime _lastScanTime = DateTime.MinValue;
        private string _lastScannedValue = string.Empty;

        public ScannerViewModel(
            IScannerService scannerService,
            ISessionService sessionService,
            IEventAggregator eventAggregator)
        {
            _scannerService = scannerService;
            _sessionService = sessionService;
            _eventAggregator = eventAggregator; 
            Title = "Scanner";
        }

        [ObservableProperty] private int _sessionId;
        [ObservableProperty] private string _sessionName = string.Empty;
        [ObservableProperty] private string _sessionRoom = string.Empty;
        [ObservableProperty] private string _lastScannedCode = string.Empty;
        [ObservableProperty] private string _lastBarcodeType = string.Empty;
        [ObservableProperty] private int _scanCount;
        [ObservableProperty] private bool _isDuplicate;
        [ObservableProperty] private bool _isScanning = true;
        [ObservableProperty] private bool _showDuplicateAlert;
        [ObservableProperty] private bool _showSuccessAlert;
        [ObservableProperty] private string _scanStatusMessage = "Ready to scan";
        [ObservableProperty] private ObservableCollection<ScannedItem> _recentScans = new();

        [RelayCommand]
        private async Task LoadSessionInfoAsync()
        {
            await ExecuteBusyAsync(async () =>
            {
                var session = await _sessionService.GetSessionByIdAsync(SessionId);
                if (session == null)
                {
                    await ShowAlertAsync("Error", "Session not found.");
                    await NavigateBackAsync();
                    return;
                }

                SessionName = session.DisplayName;
                SessionRoom = session.DisplayName;
                ScanCount = session.ScanCount;
                Title = $"Scan: {session.DisplayName}";
            }, "Loading session");
        }

        [RelayCommand]
        private async Task ProcessBarcodeAsync(string? barcodeData)
        {
            if (string.IsNullOrWhiteSpace(barcodeData) || !IsScanning || IsBusy)
                return;

            var now = DateTime.Now;
            if (barcodeData == _lastScannedValue &&
                (now - _lastScanTime).TotalMilliseconds < 1500)
                return;

            _lastScannedValue = barcodeData;
            _lastScanTime = now;

            await ExecuteBusyAsync(async () =>
            {
                var (success, message, item) = await _scannerService.ProcessScanAsync(
                    SessionId, barcodeData, LastBarcodeType);

                LastScannedCode = barcodeData;
                ScanStatusMessage = message;

                if (success && item != null)
                {
                    IsDuplicate = false;
                    ShowDuplicateAlert = false;
                    ShowSuccessAlert = true;
                    ScanCount++;

                    RecentScans.Insert(0, item);
                    if (RecentScans.Count > 10)
                        RecentScans.RemoveAt(RecentScans.Count - 1);

                    await _scannerService.ProvideFeedbackAsync(true);

                    _ = Task.Delay(1500).ContinueWith(_ =>
                        MainThread.BeginInvokeOnMainThread(() => ShowSuccessAlert = false));
                }
                else
                {
                    var isDuplicate = message.StartsWith("Duplicate:", StringComparison.OrdinalIgnoreCase);
                    IsDuplicate = isDuplicate;
                    ShowDuplicateAlert = isDuplicate;
                    ShowSuccessAlert = false;

                    await _scannerService.ProvideFeedbackAsync(false);

                    _ = Task.Delay(2500).ContinueWith(_ =>
                        MainThread.BeginInvokeOnMainThread(() => ShowDuplicateAlert = false));
                }
            }, "Processing scan");
        }

        [RelayCommand]
        private void ToggleScan()
        {
            IsScanning = !IsScanning;
            ScanStatusMessage = IsScanning ? "Scanning..." : "Paused";
        }

        [RelayCommand]
        private async Task ViewHistoryAsync()
        {
            await NavigateToAsync("ScanHistory", new Dictionary<string, object>
            {
                { "SessionId", SessionId }
            });
        }

        [RelayCommand]
        private async Task GoBackAsync()
        {
            await NavigateBackAsync();
        }
    }
}
