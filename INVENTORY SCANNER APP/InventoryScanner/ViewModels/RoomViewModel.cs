using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryScanner.Models;
using InventoryScanner.Services.Interfaces;
using InventoryLocation = InventoryScanner.Models.Location;

namespace InventoryScanner.ViewModels
{
    public partial class RoomViewModel : BaseViewModel
    {
        private readonly ISetupService _setupService;
        private readonly IRoomService _roomService;

        public RoomViewModel(ISetupService setupService, IRoomService roomService)
        {
            _setupService = setupService;
            _roomService = roomService;
            Title = "Rooms";
        }

        [ObservableProperty] private string _roomName = string.Empty;
        [ObservableProperty] private Building? _selectedBuilding;
        [ObservableProperty] private InventoryLocation? _selectedLocation;
        [ObservableProperty] private Room? _selectedRoom;
        [ObservableProperty] private ObservableCollection<Building> _buildings = new();
        [ObservableProperty] private ObservableCollection<InventoryLocation> _locations = new();
        [ObservableProperty] private ObservableCollection<Room> _rooms = new();
        [ObservableProperty] private string _roomStatusMessage = string.Empty;

        partial void OnSelectedBuildingChanged(Building? value) => _ = LoadLocationsAsync(value?.BuildingId ?? 0);
        partial void OnSelectedLocationChanged(InventoryLocation? value) => _ = LoadRoomsAsync(value?.LocationId ?? 0);

        [RelayCommand]
        private async Task LoadAsync()
        {
            Buildings.Clear();
            foreach (var building in await _setupService.GetBuildingsAsync())
                Buildings.Add(building);

            SelectedBuilding = Buildings.FirstOrDefault();
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (SelectedLocation == null)
            {
                RoomStatusMessage = "Select a location first.";
                return;
            }

            var (success, message) = await _roomService.SaveRoomAsync(new Room
            {
                RoomName = RoomName,
                LocationId = SelectedLocation.LocationId
            });

            RoomStatusMessage = message;
            if (success)
            {
                RoomName = string.Empty;
                await LoadRoomsAsync(SelectedLocation.LocationId);
            }
        }

        private async Task LoadLocationsAsync(int buildingId)
        {
            Locations.Clear();
            Rooms.Clear();
            SelectedLocation = null;
            SelectedRoom = null;

            if (buildingId <= 0) return;

            foreach (var location in await _setupService.GetLocationsAsync(buildingId))
                Locations.Add(location);

            SelectedLocation = Locations.FirstOrDefault();
        }

        private async Task LoadRoomsAsync(int locationId)
        {
            Rooms.Clear();
            SelectedRoom = null;

            if (locationId <= 0) return;

            foreach (var room in await _roomService.GetRoomsAsync(locationId))
                Rooms.Add(room);
        }
    }
}
