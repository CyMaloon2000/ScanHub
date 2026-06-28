using SQLite;

namespace InventoryScanner.Models
{
    [Table("Location")]
    public class Location
    {
        [PrimaryKey, AutoIncrement]
        public int LocationId { get; set; }

        [MaxLength(200), NotNull]
        public string LocationName { get; set; } = string.Empty;

        [Indexed, NotNull]
        public int BuildingId { get; set; }
    }
}
