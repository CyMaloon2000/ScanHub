using System.Threading.Tasks;
using InventoryScanner.Models;

namespace InventoryScanner.Services.Interfaces
{
    /// <summary>
    /// Interface for barcode/QR code scanning operations.
    /// </summary>
    public interface IScannerService
    {
        Task<(bool Success, string Message, ScannedItem? Item)> ProcessScanAsync(
            int sessionId, string barcodeValue, string barcodeType);
        Task<bool> IsDuplicateAsync(int sessionId, string barcodeValue);
        Task ProvideFeedbackAsync(bool success);
    }
}
