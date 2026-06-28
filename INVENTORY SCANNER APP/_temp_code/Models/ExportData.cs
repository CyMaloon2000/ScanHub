using System;
using System.Collections.Generic;

namespace InventoryScanner.Models
{
    /// <summary>
    /// Data transfer object used for import/export operations.
    /// Contains complete session data with associated scanned items.
    /// </summary>
    public class ExportData
    {
        public string ExportVersion { get; set; } = "1.0";
        public DateTime ExportDate { get; set; } = DateTime.Now;
        public string ExportedBy { get; set; } = string.Empty;
        public List<SessionExportItem> Sessions { get; set; } = new();
    }

    /// <summary>
    /// Represents a single session with its scanned items for export.
    /// </summary>
    public class SessionExportItem
    {
        public InventorySession Session { get; set; } = new();
        public List<ScannedItem> Items { get; set; } = new();
    }
}
