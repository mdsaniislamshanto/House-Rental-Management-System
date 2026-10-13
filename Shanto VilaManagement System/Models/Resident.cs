using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shanto_VilaManagement_System.Models
{
    public class Resident
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "রুম নির্বাচন করা আবশ্যক")]
        [Display(Name = "রুম")]
        public int RoomId { get; set; }

        public virtual Room? Room { get; set; }

        [Required(ErrorMessage = "বাসিন্দার নাম প্রদান করা আবশ্যক")]
        [Display(Name = "নাম")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "ছবি")]
        [StringLength(255)]
        public string? Photo { get; set; }

        [Display(Name = "এনআইডি (সামনের অংশ)")]
        [StringLength(255)]
        public string? NIDFront { get; set; }

        [Display(Name = "এনআইডি (পেছনের অংশ)")]
        [StringLength(255)]
        public string? NIDBack { get; set; }

        [Required(ErrorMessage = "মোবাইল নম্বর প্রদান করা আবশ্যক")]
        [Display(Name = "মোবাইল নম্বর")]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "বর্তমান ঠিকানা")]
        [StringLength(255)]
        public string? CurrentAddress { get; set; }

        [Display(Name = "স্থায়ী ঠিকানা")]
        [StringLength(255)]
        public string? PermanentAddress { get; set; }

        [Display(Name = "পেশা")]
        [StringLength(100)]
        public string? Profession { get; set; }

        [Display(Name = "চাকরি/প্রতিষ্ঠানের নাম")]
        [StringLength(150)]
        public string? Organization { get; set; }

        [Display(Name = "জন্মতারিখ / বয়স")]
        [StringLength(50)]
        public string? DateOfBirthOrAge { get; set; }

        [Required(ErrorMessage = "সম্পর্ক নির্বাচন করুন")]
        [Display(Name = "সম্পর্ক")]
        [StringLength(50)]
        public string Relationship { get; set; } = "প্রধান ভাড়াটিয়া";

        [Display(Name = "প্রধান ভাড়াটিয়া")]
        public bool IsPrimaryTenant { get; set; } = false;

        [Display(Name = "অগ্রিম জমার পরিমাণ")]
        [Column(TypeName = "decimal(12,2)")]
        public decimal AdvanceAmount { get; set; } = 0.00m;

        [Display(Name = "যোগদানের তারিখ")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "বর্তমানে বসবাসরত")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "রুম ছাড়ার তারিখ")]
        public DateTime? LeftAt { get; set; }
    }
}
