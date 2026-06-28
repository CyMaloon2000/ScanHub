using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryScanner.Helpers;
using InventoryScanner.Services.Interfaces;

namespace InventoryScanner.ViewModels
{
    /// <summary>
    /// ViewModel for the application settings page. Handles theme preference management and catalog imports.
    /// </summary>
    public partial class SettingsViewModel : BaseViewModel
    {
        private static readonly string[] _themeOptions = { "System", "Light", "Dark" };
        private readonly ISetupService _setupService;

        public SettingsViewModel(ISetupService setupService)
        {
            _setupService = setupService;
            Title = "Settings";

            _selectedTheme = NormalizeThemePreference(Preferences.Get("theme_preference", "System"));
        }

        [ObservableProperty]
        private string _selectedTheme = "System";

        [ObservableProperty]
        private string _catalogFilePath = string.Empty;

        [ObservableProperty]
        private string _catalogStatusMessage = string.Empty;

        [ObservableProperty]
        private bool _isCatalogImporting;

        [ObservableProperty]
        private string _databaseResetConfirmation = string.Empty;

        [ObservableProperty]
        private string _databaseResetStatusMessage = string.Empty;

        [ObservableProperty]
        private bool _isDatabaseResetting;

        public string[] ThemeOptions { get; } = _themeOptions;

        partial void OnSelectedThemeChanged(string value)
        {
            ApplyThemePreference(value);
        }

        /// <summary>
        /// Changes the active user application theme preference.
        /// </summary>
        [RelayCommand]
        private void ApplyTheme()
        {
            ApplyThemePreference(SelectedTheme);
        }

        [RelayCommand]
        private async Task PickCatalogFileAsync()
        {
            try
            {
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Select catalog JSON file",
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
                var cacheFileName = $"catalog-import-{DateTime.UtcNow:yyyyMMddHHmmss}.json";
                var cachedPath = Path.Combine(FileSystem.CacheDirectory, cacheFileName);

                await using (var destinationStream = File.Create(cachedPath))
                {
                    await sourceStream.CopyToAsync(destinationStream);
                }

                CatalogFilePath = cachedPath;
                CatalogStatusMessage = $"Selected: {result.FileName}";
            }
            catch (Exception ex)
            {
                CatalogStatusMessage = $"Unable to select file: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task ImportCatalogAsync()
        {
            if (string.IsNullOrWhiteSpace(CatalogFilePath))
            {
                CatalogStatusMessage = "Please choose a catalog JSON file first.";
                return;
            }

            var confirmed = await ShowConfirmAsync(
                "Import Catalog",
                "This will replace the current Buildings, Locations, Rooms, and Offices with the selected file. Continue?",
                "Import",
                "Cancel");

            if (!confirmed)
                return;

            IsCatalogImporting = true;
            try
            {
                var (success, message, _, _, _, _) = await _setupService.ImportSetupAsync(CatalogFilePath);
                CatalogStatusMessage = message;
            }
            catch (Exception ex)
            {
                CatalogStatusMessage = $"Import failed: {ex.Message}";
            }
            finally
            {
                IsCatalogImporting = false;
            }
        }

        [RelayCommand]
        private async Task ResetDatabaseAsync()
        {
            if (!string.Equals(DatabaseResetConfirmation?.Trim(), "CONFIRM", StringComparison.Ordinal))
            {
                DatabaseResetStatusMessage = "Type CONFIRM exactly before deleting the database.";
                return;
            }

            var confirmed = await ShowConfirmAsync(
                "Delete All Local Data",
                "This will permanently delete all sessions, scans, buildings, locations, rooms, offices, and users from this device. Continue?",
                "Delete All",
                "Cancel");

            if (!confirmed)
                return;

            IsDatabaseResetting = true;
            try
            {
                var (success, message) = await _setupService.ClearAllDataAsync();
                DatabaseResetStatusMessage = message;

                if (!success)
                    return;

                DatabaseResetConfirmation = string.Empty;
                SecureStorage.Default.Remove(Constants.CurrentUserKey);
                SecureStorage.Default.Remove(Constants.SessionTokenKey);
                SecureStorage.Default.Remove(Constants.IsAuthenticatedKey);

                await ShowAlertAsync(
                    "Database Cleared",
                    "All local data has been deleted. Please import the required catalog data again.");

                await Shell.Current.GoToAsync("//Setup");
            }
            finally
            {
                IsDatabaseResetting = false;
            }
        }

        private static string NormalizeThemePreference(string? theme)
        {
            return Array.Exists(_themeOptions, option => option == theme)
                ? theme!
                : "System";
        }

        private void ApplyThemePreference(string? selectedTheme)
        {
            if (Application.Current == null) return;

            var normalizedTheme = NormalizeThemePreference(selectedTheme);

            if (SelectedTheme != normalizedTheme)
            {
                SelectedTheme = normalizedTheme;
                return;
            }

            var targetTheme = normalizedTheme switch
            {
                "Light" => AppTheme.Light,
                "Dark" => AppTheme.Dark,
                _ => AppTheme.Unspecified
            };

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (Application.Current != null && Application.Current.UserAppTheme != targetTheme)
                    Application.Current.UserAppTheme = targetTheme;
            });

            Preferences.Set("theme_preference", normalizedTheme);

            StatusMessage = $"Theme changed to {normalizedTheme}";
        }
    }
}
