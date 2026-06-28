using SQLite;

namespace InventoryScanner.Models
{
    [Table("Room")]
    public class Room
    {
        [PrimaryKey, AutoIncrement]
        public int RoomId { get; set; }

        [MaxLength(200), NotNull]
        public string RoomName { get; set; } = string.Empty;

        [Indexed, NotNull]
        public int LocationId { get; set; }
    }
}
