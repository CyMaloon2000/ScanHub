using System;

namespace InventoryScanner.Models
{
    /// <summary>
    /// Event raised when a barcode/QR code is successfully scanned.
    /// </summary>
    public class ScanCompletedEvent
    {
        public ScannedItem Item { get; set; } = new();
        public bool IsDuplicate { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Event raised when a session is created or updated.
    /// </summary>
    public class SessionUpdatedEvent
    {
        public InventorySession Session { get; set; } = new();
        public string Action { get; set; } = string.Empty; // Created, Updated, Deleted
    }

    /// <summary>
    /// Event raised when import or export completes.
    /// </summary>
    public class DataTransferEvent
    {
        public string Operation { get; set; } = string.Empty; // Import, Export
        public bool Success { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int RecordCount { get; set; }
    }

    /// <summary>
    /// Event raised when initial setup catalogs are imported.
    /// </summary>
    public class SetupCompletedEvent
    {
        public int BuildingCount { get; set; }
        public int LocationCount { get; set; }
        public int RoomCount { get; set; }
        public int OfficeCount { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Event raised when authentication state changes.
    /// </summary>
    public class AuthStateChangedEvent
    {
        public bool IsAuthenticated { get; set; }
        public string Username { get; set; } = string.Empty;
    }
}
