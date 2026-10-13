using Shanto_VilaManagement_System.Models;

namespace Shanto_VilaManagement_System.ViewModels
{
    public class ResidentsIndexViewModel
    {
        public string ActiveTab { get; set; } = "active"; // "active" or "archived"
        public List<RoomResidentsGroupViewModel> RoomGroups { get; set; } = new();
        public List<RoomArchivedGroupViewModel> ArchivedRoomGroups { get; set; } = new();
        public List<Resident> ArchivedResidents { get; set; } = new();
        public int TotalActiveResidentsCount { get; set; }
        public int TotalArchivedResidentsCount { get; set; }
        public int? SelectedRoomId { get; set; }
        public List<Room> AllRooms { get; set; } = new();
    }

    public class RoomResidentsGroupViewModel
    {
        public Room Room { get; set; } = null!;
        public Resident? PrimaryTenant { get; set; }
        public List<Resident> ActiveResidents { get; set; } = new();
        public int TotalActiveCount => ActiveResidents.Count;
        public int AdultCount => ActiveResidents.Count(r => !r.Relationship.Contains("ছেলে") && !r.Relationship.Contains("মেয়ে") && !r.Relationship.Contains("শিশু"));
        public int ChildCount => ActiveResidents.Count(r => r.Relationship.Contains("ছেলে") || r.Relationship.Contains("মেয়ে") || r.Relationship.Contains("শিশু"));
    }

    public class RoomArchivedGroupViewModel
    {
        public Room Room { get; set; } = null!;
        public Resident? PrimaryTenant { get; set; }
        public List<Resident> ArchivedResidents { get; set; } = new();
        public int TotalCount => ArchivedResidents.Count;
        public DateTime? LatestLeftAt => ArchivedResidents.Max(r => r.LeftAt);
    }
}
