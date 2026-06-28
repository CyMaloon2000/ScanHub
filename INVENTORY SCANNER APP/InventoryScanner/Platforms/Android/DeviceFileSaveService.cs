using Android.Content;
using Android.OS;
using Android.Provider;
using InventoryScanner.Services.Interfaces;
using System.Runtime.Versioning;
using Application = Android.App.Application;
using Environment = Android.OS.Environment;

namespace InventoryScanner.Platforms.Android
{
    /// <summary>
    /// Saves files to the public Downloads collection using Android scoped storage.
    /// </summary>
    public class DeviceFileSaveService : IDeviceFileSaveService
    {
        private const string ExportSubfolder = "InventoryScanner";

        public async Task<(bool Success, string FilePath, string Message)> SaveFileAsync(
            string sourceFilePath,
            string fileName,
            string mimeType,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
                    return (false, string.Empty, "Export file was not found.");

                if (string.IsNullOrWhiteSpace(fileName))
                    fileName = Path.GetFileName(sourceFilePath);

                if (OperatingSystem.IsAndroidVersionAtLeast(29))
                    return await SaveWithMediaStoreAsync(sourceFilePath, fileName, mimeType, cancellationToken);

                return await SaveToLegacyDownloadsAsync(sourceFilePath, fileName, cancellationToken);
            }
            catch (System.OperationCanceledException)
            {
                return (false, string.Empty, "Save was cancelled.");
            }
            catch (Exception ex)
            {
                return (false, string.Empty, $"Could not save export to device storage: {ex.Message}");
            }
        }

        [SupportedOSPlatform("android29.0")]
        private static async Task<(bool Success, string FilePath, string Message)> SaveWithMediaStoreAsync(
            string sourceFilePath,
            string fileName,
            string mimeType,
            CancellationToken cancellationToken)
        {
            var resolver = Application.Context.ContentResolver;
            if (resolver == null)
                return (false, string.Empty, "Android content resolver is unavailable.");

            var values = new ContentValues();
            values.Put(MediaStore.IMediaColumns.DisplayName, fileName);
            values.Put(MediaStore.IMediaColumns.MimeType, mimeType);
            values.Put(MediaStore.IMediaColumns.RelativePath, $"{Environment.DirectoryDownloads}/{ExportSubfolder}");
            values.Put(MediaStore.IMediaColumns.IsPending, 1);

            var uri = resolver.Insert(MediaStore.Downloads.ExternalContentUri, values);
            if (uri == null)
                return (false, string.Empty, "Could not create a Downloads file for the export.");

            try
            {
                await using var sourceStream = File.OpenRead(sourceFilePath);
                await using var destinationStream = resolver.OpenOutputStream(uri);

                if (destinationStream == null)
                    return (false, string.Empty, "Could not open the Downloads file for writing.");

                await sourceStream.CopyToAsync(destinationStream, cancellationToken);
                await destinationStream.FlushAsync(cancellationToken);

                values.Clear();
                values.Put(MediaStore.IMediaColumns.IsPending, 0);
                resolver.Update(uri, values, null, null);

                var visiblePath = $"Downloads/{ExportSubfolder}/{fileName}";
                return (true, visiblePath, $"Export saved to {visiblePath}.");
            }
            catch
            {
                resolver.Delete(uri, null, null);
                throw;
            }
        }

        private static async Task<(bool Success, string FilePath, string Message)> SaveToLegacyDownloadsAsync(
            string sourceFilePath,
            string fileName,
            CancellationToken cancellationToken)
        {
            var downloads = Environment.GetExternalStoragePublicDirectory(Environment.DirectoryDownloads);
            if (downloads?.AbsolutePath == null)
                return (false, string.Empty, "Downloads folder is unavailable.");

            var exportDirectory = Path.Combine(downloads.AbsolutePath, ExportSubfolder);
            Directory.CreateDirectory(exportDirectory);

            var destinationPath = Path.Combine(exportDirectory, fileName);
            await using var sourceStream = File.OpenRead(sourceFilePath);
            await using var destinationStream = File.Create(destinationPath);
            await sourceStream.CopyToAsync(destinationStream, cancellationToken);

            return (true, destinationPath, $"Export saved to {destinationPath}.");
        }
    }
}
