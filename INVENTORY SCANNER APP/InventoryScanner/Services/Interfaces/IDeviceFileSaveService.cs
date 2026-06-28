using System.Threading;
using System.Threading.Tasks;

namespace InventoryScanner.Services.Interfaces
{
    /// <summary>
    /// Saves app-generated files into user-accessible device storage.
    /// </summary>
    public interface IDeviceFileSaveService
    {
        Task<(bool Success, string FilePath, string Message)> SaveFileAsync(
            string sourceFilePath,
            string fileName,
            string mimeType,
            CancellationToken cancellationToken = default);
    }
}
