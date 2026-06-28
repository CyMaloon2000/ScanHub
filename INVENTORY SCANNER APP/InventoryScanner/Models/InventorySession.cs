using SQLite;
using System;

namespace InventoryScanner.Models
{
    /// <summary>
    /// Represents a normalized inventory scan session tied to a building, location, and optional office.
    /// </summary>
    [Table("InventoryScanSession")]
    public class InventorySession
    {
        [PrimaryKey, AutoIncrement]
        public int SessionId { get; set; }

        [MaxLength(200)]
        public string SessionName { get; set; } = string.Empty;

        [Indexed, NotNull]
        public int BuildingId { get; set; }

        [Indexed, NotNull]
        public int LocationId { get; set; }

        [Indexed, NotNull]
        public int RoomId { get; set; }

        [Indexed]
        public int? OfficeId { get; set; }

        [Ignore]
        public string BuildingName { get; set; } = string.Empty;

        [Ignore]
        public string LocationName { get; set; } = string.Empty;

        [Ignore]
        public string RoomName { get; set; } = string.Empty;

        [Ignore]
        public string OfficeName { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(50), NotNull]
        public string Status { get; set; } = SessionStatus.Draft;

        [MaxLength(100)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime UpdatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Not stored in DB - populated at runtime for display.
        /// </summary>
        [Ignore]
        public int ScanCount { get; set; }

        /// <summary>
        /// Total expected items in this room.
        /// </summary>
        public int ExpectedCount { get; set; } = 30;

        /// <summary>
        /// Not stored in DB - progress value (0 to 1) for ProgressBar.
        /// </summary>
        [Ignore]
        public double ScanProgress => ExpectedCount > 0 ? (double)ScanCount / ExpectedCount : 0.0;

        /// <summary>
        /// Not stored in DB - count of missing items.
        /// </summary>
        [Ignore]
        public int MissingCount => Math.Max(0, ExpectedCount - ScanCount);

        /// <summary>
        /// Human readable label for lists and scan headers.
        /// </summary>
        [Ignore]
        public string DisplayName
        {
            get
            {
                var officePart = string.IsNullOrWhiteSpace(OfficeName) ? string.Empty : $" / {OfficeName}";
                return $"{BuildingName} - {LocationName} / {RoomName}{officePart}";
            }
        }
    }

    /// <summary>
    /// Constants for inventory session status values.
    /// </summary>
    public static class SessionStatus
    {
        public const string Draft = "Draft";
        public const string InProgress = "In Progress";
        public const string Completed = "Completed";
        public const string Archived = "Archived";

        public static readonly string[] All = { Draft, InProgress, Completed, Archived };

        public static bool IsValidTransition(string from, string to)
        {
            return (from, to) switch
            {
                (Draft, InProgress) => true,
                (Draft, Completed) => true,
                (InProgress, Completed) => true,
                (InProgress, Draft) => true,
                (Completed, Archived) => true,
                (Completed, InProgress) => true,
                _ => from == to
            };
        }
    }
}
