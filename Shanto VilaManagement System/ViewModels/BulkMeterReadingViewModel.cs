using System.ComponentModel.DataAnnotations;
using Shanto_VilaManagement_System.Helpers;

namespace Shanto_VilaManagement_System.ViewModels
{
    public class BulkMeterReadingViewModel
    {
        [Required]
        [Display(Name = "বছর")]
        public int Year { get; set; } = BanglaHelper.GetDefaultBillingPeriod().Year;

        [Required]
        [Range(1, 12)]
        [Display(Name = "মাস")]
        public int Month { get; set; } = BanglaHelper.GetDefaultBillingPeriod().Month;

        public decimal UnitPrice { get; set; }

        public List<RoomReadingItemViewModel> Items { get; set; } = new();
    }

    public class RoomReadingItemViewModel
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string? MeterNumber { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public decimal RoomRent { get; set; }

        public decimal PreviousReading { get; set; }
        public decimal? CurrentReading { get; set; }
        public decimal PreviousDue { get; set; } = 0m;
        public decimal OtherBill { get; set; } = 0m;
        public decimal UsedUnit => (CurrentReading.HasValue && CurrentReading >= PreviousReading) ? (CurrentReading.Value - PreviousReading) : 0;
        public decimal CalculatedElectricityBill(decimal unitPrice) => UsedUnit * unitPrice;
        public decimal CalculatedTotalBill(decimal unitPrice) => RoomRent + CalculatedElectricityBill(unitPrice) + OtherBill + PreviousDue;

        public bool HasExistingBill { get; set; }
        public int? ExistingBillId { get; set; }
        public string? ExistingPaymentStatus { get; set; }
        public bool IsEditMode { get; set; }
    }
}
