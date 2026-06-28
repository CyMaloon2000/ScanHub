using System.Collections.Generic;
using System.Threading.Tasks;
using InventoryScanner.Models;

namespace InventoryScanner.Services.Interfaces
{
    /// <summary>
    /// Interface for data export operations.
    /// </summary>
    public interface IExportService
    {
        Task<(bool Success, string FilePath, string Message)> ExportSessionsAsync(
            List<int> sessionIds, string format);
        Task<(bool Success, string FilePath, string Message)> ExportAllSessionsAsync(string format);
        string[] GetSupportedFormats();
    }
}
