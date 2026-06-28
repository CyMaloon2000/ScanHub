using SQLite;
using System;

namespace InventoryScanner.Models
{
    /// <summary>
    /// Represents a single scanned barcode/QR code item within an inventory session.
    /// </summary>
    [Table("ScannedItem")]
    public class ScannedItem
    {
        [PrimaryKey, AutoIncrement]
        public int ScanId { get; set; }

        [Indexed, NotNull]
        public int SessionId { get; set; }

        [Indexed, MaxLength(500), NotNull]
        public string UniqueIdentifier { get; set; } = string.Empty;

        [MaxLength(50)]
        public string BarcodeType { get; set; } = string.Empty;

        public DateTime ScannedAt { get; set; } = DateTime.Now;

        [MaxLength(500)]
        public string Remarks { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Status { get; set; } = "Active";
    }
}
