using Shanto_VilaManagement_System.Models;

namespace Shanto_VilaManagement_System.ViewModels
{
    public class PaymentFilterViewModel
    {
        public string? Status { get; set; } // "All", "Paid", "Unpaid"
        public int? Year { get; set; }
        public int? Month { get; set; }
        public int? RoomId { get; set; }
        public string? SearchTenant { get; set; }

        public List<MonthlyBill> Bills { get; set; } = new();
        public List<Room> Rooms { get; set; } = new();

        public decimal TotalAmount => Bills.Sum(b => b.TotalBill);
        public decimal TotalPaidAmount => Bills.Sum(b => b.EffectivePaidAmount);
        public decimal TotalDueAmount => Bills.Sum(b => b.EffectiveDueAmount);
    }
}
