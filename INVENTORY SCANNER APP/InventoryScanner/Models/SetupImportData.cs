using System.Collections.Generic;

namespace InventoryScanner.Models
{
    public class SetupImportData
    {
        public List<SetupImportBuilding> Buildings { get; set; } = new();
        public List<SetupImportLocation> Locations { get; set; } = new();
        public List<SetupImportRoom> Rooms { get; set; } = new();
        public List<SetupImportOffice> Offices { get; set; } = new();
    }

    public class SetupImportBuilding
    {
        public int BuildingId { get; set; }
        public string BuildingName { get; set; } = string.Empty;
    }

    public class SetupImportLocation
    {
        public int LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public int? BuildingId { get; set; }
    }

    public class SetupImportRoom
    {
        public int RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public int? LocationId { get; set; }
    }

    public class SetupImportOffice
    {
        public int OfficeId { get; set; }
        public string OfficeName { get; set; } = string.Empty;
    }
}
