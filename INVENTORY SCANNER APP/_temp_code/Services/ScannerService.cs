using System;
using System.Threading.Tasks;
using InventoryScanner.Helpers;
using InventoryScanner.Models;
using InventoryScanner.Repositories.Interfaces;
using InventoryScanner.Services.Interfaces;

namespace InventoryScanner.Services
{
    /// <summary>
    /// Handles barcode/QR code scan processing, duplicate detection, and feedback.
    /// </summary>
    public class ScannerService : IScannerService
    {
        private readonly IScannedItemRepository _scannedItemRepository;
        private readonly ISessionRepository _sessionRepository;
        private readonly IEventAggregator _eventAggregator;

        public ScannerService(
            IScannedItemRepository scannedItemRepository,
            ISessionRepository sessionRepository,
            IEventAggregator eventAggregator)
        {
            _scannedItemRepository = scannedItemRepository;
            _sessionRepository = sessionRepository;
            _eventAggregator = eventAggregator;
        }

        /// <inheritdoc/>
        public async Task<(bool Success, string Message, ScannedItem? Item)> ProcessScanAsync(
            int sessionId, string barcodeValue, string barcodeType)
        {
            try
            {
                // Validate barcode
                if (!ValidationHelper.IsValidBarcode(barcodeValue))
                    return (false, "Invalid barcode format.", null);

                // Check session exists
                var sessionExists = await _sessionRepository.ExistsAsync(sessionId);
                if (!sessionExists)
                    return (false, "Session not found.", null);

                // Check duplicate - defense in depth at service layer
                if (await IsDuplicateAsync(sessionId, barcodeValue))
                {
                    _eventAggregator.Publish(new ScanCompletedEvent
                    {
                        IsDuplicate = true,
                        Message = $"Duplicate: '{barcodeValue}' already exists in this session."
                    });
                    return (false, $"Duplicate: '{barcodeValue}' already exists in this session.", null);
                }

                // Create and insert the scanned item
                var item = new ScannedItem
                {
                    SessionId = sessionId,
                    UniqueIdentifier = barcodeValue.Trim(),
                    BarcodeType = barcodeType,
                    ScannedAt = DateTime.Now,
                    Status = "Active"
                };

                try
                {
                    await _scannedItemRepository.InsertAsync(item);
                }
                catch (SQLite.SQLiteException ex) when (ex.Message.Contains("UNIQUE constraint"))
                {
                    // Database-level duplicate enforcement (defense in depth)
                    return (false, $"Duplicate: '{barcodeValue}' already exists in this session.", null);
                }

                _eventAggregator.Publish(new ScanCompletedEvent
                {
                    Item = item,
                    IsDuplicate = false,
                    Message = $"Scanned: {barcodeValue}"
                });

                return (true, $"Scanned successfully: {barcodeValue}", item);
            }
            catch (Exception ex)
            {
                return (false, $"Scan failed: {ex.Message}", null);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> IsDuplicateAsync(int sessionId, string barcodeValue)
        {
            return await _scannedItemRepository.ExistsInSessionAsync(sessionId, barcodeValue.Trim());
        }

        /// <inheritdoc/>
        public async Task ProvideFeedbackAsync(bool success)
        {
            try
            {
                if (success)
                {
                    // Vibrate briefly on success
                    Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(200));
                }
                else
                {
                    // Vibrate longer pattern for error/duplicate
                    Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500));
                }
            }
            catch (Exception)
            {
                // Vibration may not be supported on all devices
            }

            await Task.CompletedTask;
        }
    }
}
