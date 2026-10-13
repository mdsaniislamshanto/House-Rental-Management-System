using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shanto_VilaManagement_System.Models
{
    public class Room
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "রুম নম্বর প্রদান করা আবশ্যক")]
        [Display(Name = "রুম নম্বর")]
        [StringLength(50)]
        public string RoomNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "মাসিক ভাড়া প্রদান করা আবশ্যক")]
        [Display(Name = "মাসিক ভাড়া (টাকা)")]
        [Column(TypeName = "decimal(12, 2)")]
        [Range(0, 1000000, ErrorMessage = "ভাড়া ০ বা তার বেশি হতে হবে")]
        public decimal RoomRent { get; set; }

        [Display(Name = "অন্যান্য বিল (টাকা)")]
        [Column(TypeName = "decimal(12, 2)")]
        [Range(0, 100000, ErrorMessage = "টাকার পরিমাণ ০ বা তার বেশি হতে হবে")]
        public decimal? OtherBill { get; set; }

        [Display(Name = "মিটার নম্বর")]
        [StringLength(100)]
        public string? MeterNumber { get; set; }

        [Display(Name = "পূর্বের / প্রারম্ভিক মিটার রিডিং")]
        [Column(TypeName = "decimal(12, 2)")]
        [Range(0, 10000000, ErrorMessage = "মিটার রিডিং ০ বা তার বেশি হতে হবে")]
        public decimal InitialMeterReading { get; set; } = 0;

        [Required]
        [Display(Name = "অবস্থা")]
        [StringLength(50)]
        public string Status { get; set; } = "Vacant"; // Occupied, Vacant, Inactive

        [Display(Name = "তৈরির তারিখ")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        public virtual ICollection<Resident> Residents { get; set; } = new List<Resident>();
        public virtual ICollection<EmergencyContact> EmergencyContacts { get; set; } = new List<EmergencyContact>();
        public virtual ICollection<MonthlyBill> MonthlyBills { get; set; } = new List<MonthlyBill>();

        [NotMapped]
        public Resident? PrimaryTenant => Residents?.Where(r => r.IsActive).FirstOrDefault(r => r.IsPrimaryTenant) ?? Residents?.FirstOrDefault(r => r.IsActive);

        [NotMapped]
        public int ActiveResidentsCount => Residents?.Count(r => r.IsActive) ?? 0;

        [NotMapped]
        public string StatusBangla => Status switch
        {
            "Occupied" => "ভাড়া দেওয়া",
            "Vacant" => "খালি",
            "Inactive" => "নিষ্ক্রিয়",
            _ => Status
        };

        [NotMapped]
        public string StatusBadgeClass => Status switch
        {
            "Occupied" => "badge-occupied",
            "Vacant" => "badge-vacant",
            "Inactive" => "badge-inactive",
            _ => "bg-secondary"
        };
    }
}
