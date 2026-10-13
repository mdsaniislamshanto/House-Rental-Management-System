using Shanto_VilaManagement_System.Models;

namespace Shanto_VilaManagement_System.ViewModels
{
    public class DashboardViewModel
    {
        public string HouseName { get; set; } = "শান্ত ভিলা";
        public string HouseAddress { get; set; } = string.Empty;
        public decimal CurrentUnitPrice { get; set; }

        public int TotalRooms { get; set; }
        public int OccupiedRooms { get; set; }
        public int VacantRooms { get; set; }
        public int InactiveRooms { get; set; }

        public int CurrentYear { get; set; }
        public int CurrentMonthNumber { get; set; }
        public string CurrentMonthBangla { get; set; } = string.Empty;

        public decimal TotalRoomRent { get; set; }
        public decimal TotalElectricityBill { get; set; }
        public decimal TotalBillAmount { get; set; }

        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }

        public int PaidRoomsCount { get; set; }
        public int DueRoomsCount { get; set; }

        public List<MonthlyBill> RecentBills { get; set; } = new();
        public List<Room> Rooms { get; set; } = new();
    }
}
