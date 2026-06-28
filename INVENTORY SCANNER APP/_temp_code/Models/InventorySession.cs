using SQLite;
using System;

namespace InventoryScanner.Models
{
    /// <summary>
    /// Represents an inventory validation session for a specific room/location.
    /// </summary>
    [Table("InventorySession")]
    public class InventorySession
    {
        [PrimaryKey, AutoIncrement]
        public int SessionId { get; set; }

        [MaxLength(200)]
        public string SessionName { get; set; } = string.Empty;

        [MaxLength(200), NotNull]
        public string School { get; set; } = string.Empty;

        [MaxLength(200), NotNull]
        public string Campus { get; set; } = string.Empty;

        [MaxLength(200), NotNull]
        public string Location { get; set; } = string.Empty;

        [MaxLength(200), NotNull]
        public string Room { get; set; } = string.Empty;

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
