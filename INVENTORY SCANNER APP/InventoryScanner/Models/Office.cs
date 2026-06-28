using SQLite;

namespace InventoryScanner.Models
{
    [Table("Office")]
    public class Office
    {
        [PrimaryKey, AutoIncrement]
        public int OfficeId { get; set; }

        [MaxLength(200), NotNull]
        public string OfficeName { get; set; } = string.Empty;
    }
}
