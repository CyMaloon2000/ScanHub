using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryScanner.Models;
using InventoryScanner.Services.Interfaces;

namespace InventoryScanner.ViewModels
{
    /// <summary>
    /// ViewModel for export and import operations.
    /// </summary>
    public partial class ExportImportViewModel : BaseViewModel
    {
        private readonly IExportService _exportService;
        private readonly IImportService _importService;
        private readonly ISessionService _sessionService;
        private readonly IDeviceFileSaveService _deviceFileSaveService;

        public ExportImportViewModel(
            IExportService exportService,
            IImportService importService,
            ISessionService sessionService,
            IDeviceFileSaveService deviceFileSaveService)
        {
            _exportService = exportService;
            _importService = importService;
            _sessionService = sessionService;
            _deviceFileSaveService = deviceFileSaveService;
            Title = "Export / Import";
        }

        // --- Export ---

        [ObservableProperty]
        private ObservableCollection<InventorySession> _availableSessions = new();

        [ObservableProperty]
        private ObservableCollection<InventorySession> _selectedSessions = new();

        [ObservableProperty]
        private string _selectedFormat = "JSON";

        [ObservableProperty]
        private bool _exportAll = true;

        [ObservableProperty]
        private string _exportStatusMessage = string.Empty;

        [ObservableProperty]
        private bool _exportSuccess;

        [ObservableProperty]
        private string _lastExportPath = string.Empty;

        [ObservableProperty]
        private string _lastDeviceExportPath = string.Empty;

        // --- Import ---

        [ObservableProperty]
        private string _importFilePath = string.Empty;

        [ObservableProperty]
        private string _importStatusMessage = string.Empty;

        [ObservableProperty]
        private bool _importSuccess;

        [ObservableProperty]
        private bool _isImporting;

        [ObservableProperty]
        private bool _isExporting;

        public string[] Formats => _exportService.GetSupportedFormats();

        /// <summary>
        /// Loads available sessions for export.
        /// </summary>
        [RelayCommand]
        private async Task LoadSessionsAsync()
        {
            await ExecuteBusyAsync(async () =>
            {
                var sessions = await _sessionService.GetAllSessionsAsync();
                AvailableSessions.Clear();
                foreach (var session in sessions)
                {
                    AvailableSessions.Add(session);
                }
            }, "Loading sessions");
        }

        /// <summary>
        /// Exports selected or all sessions.
        /// </summary>
        [RelayCommand]
        private async Task ExportAsync()
        {
            IsExporting = true;
            ExportSuccess = false;
            ExportStatusMessage = "Exporting...";

            try
            {
                bool success;
                string filePath;
                string message;

                if (ExportAll)
                {
                    (success, filePath, message) = await _exportService.ExportAllSessionsAsync(SelectedFormat);
                }
                else
                {
                    if (SelectedSessions.Count == 0)
                    {
                        ExportStatusMessage = "Please select at least one session to export.";
                        return;
                    }

                    var ids = SelectedSessions.Select(s => s.SessionId).ToList();
                    (success, filePath, message) = await _exportService.ExportSessionsAsync(ids, SelectedFormat);
                }

                ExportSuccess = success;
                ExportStatusMessage = message;
                LastExportPath = filePath;

                if (success)
                {
                    var saveResult = await SaveExportedFileToDeviceAsync(filePath);
                    ExportSuccess = saveResult.Success;
                    ExportStatusMessage = saveResult.Message;
                    LastDeviceExportPath = saveResult.FilePath;

                    await ShareExportedFileAsync(filePath);
                }
            }
            catch (Exception ex)
            {
                ExportStatusMessage = $"Export failed: {ex.Message}";
            }
            finally
            {
                IsExporting = false;
            }
        }

        /// <summary>
        /// Picks a file for import.
        /// </summary>
        [RelayCommand]
        private async Task PickImportFileAsync()
        {
            try
            {
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Select Import File",
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.Android, new[] { "application/json", "*/*" } }
                    })
                });

                if (result != null)
                {
                    await using var sourceStream = await result.OpenReadAsync();
                    var cacheFileName = $"session-import-{DateTime.UtcNow:yyyyMMddHHmmss}.json";
                    var cachedPath = Path.Combine(FileSystem.CacheDirectory, cacheFileName);

                    await using (var destinationStream = File.Create(cachedPath))
                    {
                        await sourceStream.CopyToAsync(destinationStream);
                    }

                    ImportFilePath = cachedPath;
                    ImportStatusMessage = $"Selected: {result.FileName}";

                    // Validate the file
                    var (isValid, validationMsg) = await _importService.ValidateImportFileAsync(cachedPath);
                    ImportStatusMessage = validationMsg;
                }
            }
            catch (Exception ex)
            {
                ImportStatusMessage = $"Error selecting file: {ex.Message}";
            }
        }

        /// <summary>
        /// Imports data from the selected file.
        /// </summary>
        [RelayCommand]
        private async Task ImportAsync()
        {
            if (string.IsNullOrWhiteSpace(ImportFilePath))
            {
                ImportStatusMessage = "Please select a file first.";
                return;
            }

            var confirmed = await ShowConfirmAsync("Import Data",
                "This will import sessions and scanned items from the selected file. Continue?",
                "Import", "Cancel");

            if (!confirmed) return;

            IsImporting = true;
            ImportSuccess = false;
            ImportStatusMessage = "Importing...";

            try
            {
                var (success, message, sessionCount, itemCount) =
                    await _importService.ImportAsync(ImportFilePath);

                ImportSuccess = success;
                ImportStatusMessage = message;

                if (success)
                {
                    ImportFilePath = string.Empty;
                    // Reload sessions list
                    await LoadSessionsAsync();
                }
            }
            catch (Exception ex)
            {
                ImportStatusMessage = $"Import failed: {ex.Message}";
            }
            finally
            {
                IsImporting = false;
            }
        }

        /// <summary>
        /// Shares the exported file using the system share sheet.
        /// </summary>
        private async Task ShareExportedFileAsync(string filePath)
        {
            try
            {
                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Share Export File",
                    File = new ShareFile(filePath)
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Share failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Copies the app-private export into a user-accessible device folder.
        /// </summary>
        private async Task<(bool Success, string FilePath, string Message)> SaveExportedFileToDeviceAsync(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            var mimeType = GetMimeType(fileName);

            return await _deviceFileSaveService.SaveFileAsync(filePath, fileName, mimeType);
        }

        private static string GetMimeType(string fileName)
        {
            return Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".csv" => "text/csv",
                ".json" => "application/json",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
        }

        /// <summary>
        /// Navigates back.
        /// </summary>
        [RelayCommand]
        private async Task GoBackAsync()
        {
            await NavigateBackAsync();
        }
    }
}
