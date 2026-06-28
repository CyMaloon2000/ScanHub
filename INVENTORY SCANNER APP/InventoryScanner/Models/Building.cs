using SQLite;

namespace InventoryScanner.Models
{
    [Table("Building")]
    public class Building
    {
        [PrimaryKey, AutoIncrement]
        public int BuildingId { get; set; }

        [MaxLength(200), NotNull]
        public string BuildingName { get; set; } = string.Empty;
    }
}
