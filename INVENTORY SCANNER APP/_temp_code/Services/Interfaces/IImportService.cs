using System.Threading.Tasks;

namespace InventoryScanner.Services.Interfaces
{
    /// <summary>
    /// Interface for data import operations.
    /// </summary>
    public interface IImportService
    {
        Task<(bool Success, string Message, int SessionCount, int ItemCount)> ImportAsync(string filePath);
        Task<(bool IsValid, string Message)> ValidateImportFileAsync(string filePath);
    }
}
