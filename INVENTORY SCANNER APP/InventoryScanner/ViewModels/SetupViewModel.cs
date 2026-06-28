using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryScanner.Services.Interfaces;

namespace InventoryScanner.ViewModels
{
    public partial class SetupViewModel : BaseViewModel
    {
        private readonly ISetupService _setupService;

        public SetupViewModel(ISetupService setupService)
        {
            _setupService = setupService;
            Title = "Initial Setup";
        }

        [ObservableProperty]
        private string _statusText = "Import a JSON file containing Buildings, Locations, Rooms, and optional Offices. Existing catalog data will be replaced.";

        [ObservableProperty]
        private string _selectedFilePath = string.Empty;

        [ObservableProperty]
        private bool _isSetupComplete;

        [RelayCommand]
        private async Task LoadAsync()
        {
            IsSetupComplete = await _setupService.IsSetupCompleteAsync();
            if (IsSetupComplete)
                await NavigateToLoginAsync();
        }

        [RelayCommand]
        private async Task PickFileAsync()
        {
            try
            {
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Select setup JSON file",
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.Android, new[] { "application/json", "text/plain", "*/*" } },
                        { DevicePlatform.iOS, new[] { "public.json", "public.text" } },
                        { DevicePlatform.MacCatalyst, new[] { "public.json", "public.text" } },
                        { DevicePlatform.WinUI, new[] { ".json", ".txt" } }
                    })
                });

                if (result == null)
                    return;

                await using var sourceStream = await result.OpenReadAsync();
                var cacheFileName = $"setup-import-{DateTime.UtcNow:yyyyMMddHHmmss}.json";
                var cachedPath = Path.Combine(FileSystem.CacheDirectory, cacheFileName);

                await using (var destinationStream = File.Create(cachedPath))
                {
                    await sourceStream.CopyToAsync(destinationStream);
                }

                SelectedFilePath = cachedPath;
                StatusText = $"Selected: {result.FileName}";
            }
            catch (Exception ex)
            {
                StatusText = $"Unable to select file: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task ImportAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedFilePath))
            {
                StatusText = "Please choose a JSON file first.";
                return;
            }

            var confirmed = await ShowConfirmAsync(
                "Import Setup",
                "This will replace all existing Buildings, Locations, Rooms, and Offices in the local database. Continue?",
                "Import",
                "Cancel");

            if (!confirmed)
                return;

            await ExecuteBusyAsync(async () =>
            {
                var (success, message, _, _, _, _) = await _setupService.ImportSetupAsync(SelectedFilePath);
                StatusText = message;

                if (success)
                {
                    IsSetupComplete = true;
                    await NavigateToLoginAsync();
                }
            }, "Import setup");
        }

        private async Task NavigateToLoginAsync()
        {
            await Shell.Current.GoToAsync("//Login");
        }
    }
}
