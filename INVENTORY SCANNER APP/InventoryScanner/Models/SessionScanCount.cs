namespace InventoryScanner.Models
{
    /// <summary>
    /// Projection for grouped scan counts by session.
    /// </summary>
    public class SessionScanCount
    {
        public int SessionId { get; set; }
        public int ScanCount { get; set; }
    }
}
