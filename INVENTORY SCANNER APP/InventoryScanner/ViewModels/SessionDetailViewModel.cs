using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryScanner.Models;
using InventoryScanner.Repositories.Interfaces;
using InventoryScanner.Services;
using InventoryScanner.Services.Interfaces;
using InventoryLocation = InventoryScanner.Models.Location;

namespace InventoryScanner.ViewModels
{
    [QueryProperty(nameof(SessionId), "SessionId")]
    [QueryProperty(nameof(IsNew), "IsNew")]
    public partial class SessionDetailViewModel : BaseViewModel
    {
        private readonly ISessionService _sessionService;
        private readonly ISetupService _setupService;
        private readonly IAuthenticationService _authService;
        private readonly IScannedItemRepository _scannedItemRepository;
        private readonly ZxingScannerService _zxingScannerService;
        private readonly GoogleMlKitScannerService _googleMlKitScannerService;

        public SessionDetailViewModel(
            ISessionService sessionService,
            ISetupService setupService,
            IAuthenticationService authService,
            IScannedItemRepository scannedItemRepository,
            ZxingScannerService zxingScannerService,
            GoogleMlKitScannerService googleMlKitScannerService)
        {
            _sessionService = sessionService;
            _setupService = setupService;
            _authService = authService;
            _scannedItemRepository = scannedItemRepository;
            _zxingScannerService = zxingScannerService;
            _googleMlKitScannerService = googleMlKitScannerService;
            Title = "New Session";
        }

        [ObservableProperty] private int _sessionId;
        [ObservableProperty] private bool _isNew = true;
        [ObservableProperty] private string _sessionName = string.Empty;
        [ObservableProperty] private Building? _selectedBuilding;
        [ObservableProperty] private InventoryLocation? _selectedLocation;
        [ObservableProperty] private Room? _selectedRoom;
        [ObservableProperty] private Office? _selectedOffice;
        [ObservableProperty] private string _description = string.Empty;
        [ObservableProperty] private string _selectedStatus = SessionStatus.Draft;
        [ObservableProperty] private string _createdBy = string.Empty;
        [ObservableProperty] private DateTime _createdDate = DateTime.Now;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MissingCount))]
        [NotifyPropertyChangedFor(nameof(ScanProgress))]
        [NotifyPropertyChangedFor(nameof(ScanProgressPercent))]
        private int _scanCount;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MissingCount))]
        [NotifyPropertyChangedFor(nameof(ScanProgress))]
        [NotifyPropertyChangedFor(nameof(ScanProgressPercent))]
        private int _expectedCount = 30;
        [ObservableProperty] private bool _canEdit = true;
        [ObservableProperty] private bool _canStartScanning;
        [ObservableProperty] private ObservableCollection<ScannedItem> _recentScans = new();
        [ObservableProperty] private ObservableCollection<Building> _buildings = new();
        [ObservableProperty] private ObservableCollection<InventoryLocation> _locations = new();
        [ObservableProperty] private ObservableCollection<Room> _rooms = new();
        [ObservableProperty] private ObservableCollection<Office> _offices = new();

        public string[] StatusOptions => SessionStatus.All;
        public int MissingCount => Math.Max(0, ExpectedCount - ScanCount);
        public double ScanProgress => ExpectedCount > 0 ? (double)ScanCount / ExpectedCount : 0;
        public string ScanProgressPercent => $"{Math.Min(100, (int)Math.Round(ScanProgress * 100))}%";

        partial void OnSelectedBuildingChanged(Building? value)
        {
            _ = LoadLocationsAsync(value?.BuildingId ?? 0);
        }

        partial void OnSelectedLocationChanged(InventoryLocation? value)
        {
            _ = LoadRoomsAsync(value?.LocationId ?? 0);
        }

        [RelayCommand]
        private async Task LoadSessionAsync()
        {
            await ExecuteBusyAsync(async () =>
            {
                await LoadCatalogsAsync();

                if (IsNew)
                {
                    Title = "New Session";
                    CanEdit = true;
                    CanStartScanning = false;
                    CreatedBy = await _authService.GetCurrentUsernameAsync();
                    ExpectedCount = 30;
                    RecentScans.Clear();

                    SelectedBuilding = Buildings.FirstOrDefault();
                    return;
                }

                var session = await _sessionService.GetSessionByIdAsync(SessionId);
                if (session == null)
                {
                    await ShowAlertAsync("Error", "Session not found.");
                    await NavigateBackAsync();
                    return;
                }

                Title = session.DisplayName;
                SessionName = session.DisplayName;
                SelectedBuilding = Buildings.FirstOrDefault(b => b.BuildingId == session.BuildingId);
                Description = session.Description;
                SelectedStatus = session.Status;
                CreatedBy = session.CreatedBy;
                CreatedDate = session.CreatedDate;
                ExpectedCount = session.ExpectedCount;
                ScanCount = session.ScanCount;

                CanEdit = session.Status != SessionStatus.Completed &&
                          session.Status != SessionStatus.Archived;
                CanStartScanning = CanEdit;

                if (SelectedBuilding != null)
                    await LoadLocationsAsync(SelectedBuilding.BuildingId);

                SelectedLocation = Locations.FirstOrDefault(l => l.LocationId == session.LocationId);
                if (SelectedLocation != null)
                    await LoadRoomsAsync(SelectedLocation.LocationId);

                SelectedRoom = Rooms.FirstOrDefault(r => r.RoomId == session.RoomId);
                SelectedOffice = Offices.FirstOrDefault(o => o.OfficeId == session.OfficeId);

                var allItems = await _scannedItemRepository.GetBySessionIdAsync(SessionId);
                foreach (var item in allItems.OrderByDescending(i => i.ScannedAt).Take(3))
                    RecentScans.Add(item);
            }, "Loading session");
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            await ExecuteBusyAsync(async () =>
            {
                if (SelectedBuilding == null || SelectedLocation == null || SelectedRoom == null)
                {
                    await ShowAlertAsync("Missing Data", "Please select a building, location, and room.");
                    return;
                }

                var session = new InventorySession
                {
                    SessionId = IsNew ? 0 : SessionId,
                    BuildingId = SelectedBuilding.BuildingId,
                    LocationId = SelectedLocation.LocationId,
                    RoomId = SelectedRoom.RoomId,
                    OfficeId = SelectedOffice?.OfficeId,
                    Description = Description.Trim(),
                    Status = IsNew ? SessionStatus.Draft : SelectedStatus,
                    CreatedBy = string.IsNullOrWhiteSpace(CreatedBy) ? await _authService.GetCurrentUsernameAsync() : CreatedBy,
                    CreatedDate = IsNew ? DateTime.Now : CreatedDate,
                    ExpectedCount = ExpectedCount
                };

                var (success, message) = IsNew
                    ? await _sessionService.CreateSessionAsync(session)
                    : await _sessionService.UpdateSessionAsync(session);

                if (success)
                {
                    StatusMessage = message;
                    await NavigateBackAsync();
                }
                else
                {
                    await ShowAlertAsync("Error", message);
                }
            }, "Saving session");
        }

        [RelayCommand]
        private async Task StartScanningAsync()
        {
            await StartScannerAsync(_zxingScannerService);
        }

        [RelayCommand]
        private async Task StartScanningV2Async()
        {
            await StartScannerAsync(_googleMlKitScannerService);
        }

        private async Task StartScannerAsync(IScannerImplementationService scannerService)
        {
            if (IsNew)
            {
                await ShowAlertAsync("Save First", "Please save the session before scanning.");
                return;
            }

            if (SelectedStatus == SessionStatus.Draft)
                await _sessionService.UpdateStatusAsync(SessionId, SessionStatus.InProgress);

            await scannerService.StartAsync(SessionId);
        }

        [RelayCommand]
        private async Task ViewHistoryAsync()
        {
            if (IsNew) return;

            await NavigateToAsync("ScanHistory", new Dictionary<string, object>
            {
                { "SessionId", SessionId }
            });
        }

        [RelayCommand]
        private async Task CancelAsync() => await NavigateBackAsync();

        private async Task LoadCatalogsAsync()
        {
            Buildings.Clear();
            foreach (var building in await _setupService.GetBuildingsAsync())
                Buildings.Add(building);

            Offices.Clear();
            foreach (var office in await _setupService.GetOfficesAsync())
                Offices.Add(office);
        }

        private async Task LoadLocationsAsync(int buildingId)
        {
            Locations.Clear();
            Rooms.Clear();
            SelectedLocation = null;
            SelectedRoom = null;

            if (buildingId <= 0)
                return;

            foreach (var location in await _setupService.GetLocationsAsync(buildingId))
                Locations.Add(location);

            if (Locations.Count > 0)
                SelectedLocation = Locations.First();
        }

        private async Task LoadRoomsAsync(int locationId)
        {
            Rooms.Clear();
            SelectedRoom = null;

            if (locationId <= 0)
                return;

            foreach (var room in await _setupService.GetRoomsAsync(locationId))
                Rooms.Add(room);

            if (Rooms.Count > 0)
                SelectedRoom = Rooms.First();
        }
    }
}
