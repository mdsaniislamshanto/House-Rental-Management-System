using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shanto_VilaManagement_System.Models
{
    public class MonthlyBill
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "রুম")]
        public int RoomId { get; set; }

        public virtual Room? Room { get; set; }

        [Required]
        [Display(Name = "বিলের মাস")]
        [StringLength(50)]
        public string BillingMonth { get; set; } = string.Empty; // e.g. "অক্টোবর ২০২৬" or "October 2026"

        [Required]
        [Display(Name = "বছর")]
        public int BillingYear { get; set; }

        [Required]
        [Display(Name = "মাস নম্বর")]
        [Range(1, 12)]
        public int BillingMonthNumber { get; set; }

        [Required]
        [Display(Name = "ভাড়াটিয়ার নাম")]
        [StringLength(100)]
        public string TenantName { get; set; } = string.Empty; // Historical snapshot

        [Required]
        [Display(Name = "রুম ভাড়া")]
        [Column(TypeName = "decimal(12, 2)")]
        public decimal RoomRent { get; set; } // Historical snapshot

        [Required]
        [Display(Name = "পূর্বের মিটার রিডিং")]
        [Column(TypeName = "decimal(12, 2)")]
        public decimal PreviousReading { get; set; }

        [Required]
        [Display(Name = "বর্তমান মিটার রিডিং")]
        [Column(TypeName = "decimal(12, 2)")]
        public decimal CurrentReading { get; set; }

        [Required]
        [Display(Name = "ব্যবহৃত ইউনিট")]
        [Column(TypeName = "decimal(12, 2)")]
        public decimal UsedUnit { get; set; }

        [Required]
        [Display(Name = "ইউনিট মূল্য")]
        [Column(TypeName = "decimal(12, 2)")]
        public decimal UnitPrice { get; set; } // Historical snapshot

        [Required]
        [Display(Name = "বিদ্যুৎ বিল")]
        [Column(TypeName = "decimal(12, 2)")]
        public decimal ElectricityBill { get; set; }

        [Display(Name = "অন্যান্য বিল")]
        [Column(TypeName = "decimal(12, 2)")]
        public decimal OtherBill { get; set; } = 0m;

        [Display(Name = "পূর্বের বকেয়া")]
        [Column(TypeName = "decimal(12, 2)")]
        public decimal PreviousDue { get; set; } = 0m;

        [Required]
        [Display(Name = "মোট বিল")]
        [Column(TypeName = "decimal(12, 2)")]
        public decimal TotalBill { get; set; }

        [Display(Name = "পরিশোধিত টাকা")]
        [Column(TypeName = "decimal(12, 2)")]
        public decimal PaidAmount { get; set; } = 0m;

        [Display(Name = "অবশিষ্ট বকেয়া")]
        [Column(TypeName = "decimal(12, 2)")]
        public decimal DueAmount { get; set; } = 0m;

        [Required]
        [Display(Name = "পরিশোধের অবস্থা")]
        [StringLength(20)]
        public string PaymentStatus { get; set; } = "Unpaid"; // "Paid", "Partial", or "Unpaid"

        [Display(Name = "পরিশোধের তারিখ")]
        public DateTime? PaidDate { get; set; }

        [Display(Name = "তৈরির তারিখ")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public bool IsPaid => PaymentStatus == "Paid";

        [NotMapped]
        public bool IsPartial => PaymentStatus == "Partial";

        [NotMapped]
        public string PaymentStatusBangla => PaymentStatus switch
        {
            "Paid" => "পরিশোধিত",
            "Partial" => "আংশিক পরিশোধ",
            _ => "বকেয়া"
        };

        [NotMapped]
        public string PaymentBadgeClass => PaymentStatus switch
        {
            "Paid" => "badge-paid",
            "Partial" => "badge-partial",
            _ => "badge-unpaid"
        };

        [NotMapped]
        public decimal EffectivePaidAmount => PaidAmount > 0 ? PaidAmount : (IsPaid ? TotalBill : 0m);

        [NotMapped]
        public decimal EffectiveDueAmount => (PaymentStatus == "Paid") ? 0m : (DueAmount > 0 ? DueAmount : (TotalBill - EffectivePaidAmount));
    }
}
