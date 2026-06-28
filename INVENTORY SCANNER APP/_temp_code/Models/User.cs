using SQLite;
using System;

namespace InventoryScanner.Models
{
    /// <summary>
    /// Represents a user account for offline authentication.
    /// </summary>
    [Table("User")]
    public class User
    {
        [PrimaryKey, AutoIncrement]
        public int UserId { get; set; }

        [MaxLength(100), NotNull, Unique]
        public string Username { get; set; } = string.Empty;

        [MaxLength(256), NotNull]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(256), NotNull]
        public string Salt { get; set; } = string.Empty;

        [MaxLength(200)]
        public string DisplayName { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
