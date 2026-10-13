using Shanto_VilaManagement_System.Models;

namespace Shanto_VilaManagement_System.ViewModels
{
    public class SearchViewModel
    {
        public string? Query { get; set; }
        public List<Room> MatchedRooms { get; set; } = new();
        public List<Resident> MatchedResidents { get; set; } = new();
        public List<MonthlyBill> MatchedBills { get; set; } = new();
    }
}
