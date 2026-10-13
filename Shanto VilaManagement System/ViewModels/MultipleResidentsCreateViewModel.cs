using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Shanto_VilaManagement_System.ViewModels
{
    public class MultipleResidentsCreateViewModel
    {
        [Required(ErrorMessage = "রুম নির্বাচন করা আবশ্যক")]
        [Display(Name = "রুম")]
        public int RoomId { get; set; }

        [Range(0, 20, ErrorMessage = "প্রাপ্তবয়স্ক সদস্য সংখ্যা ০ থেকে ২০ এর মধ্যে হতে হবে")]
        [Display(Name = "প্রাপ্তবয়স্ক সদস্য সংখ্যা")]
        public int AdultCount { get; set; } = 1;

        [Range(0, 20)]
        [Display(Name = "শিশু / বাচ্চা সংখ্যা")]
        public int ChildCount { get; set; } = 0;

        [Display(Name = "অগ্রিম জমার পরিমাণ (টাকা)")]
        public decimal AdvanceAmount { get; set; } = 0.00m;

        public bool IsAddMember { get; set; } = false;

        public List<ResidentItemInputModel> Residents { get; set; } = new();
    }

    public class ResidentItemInputModel
    {
        [Required(ErrorMessage = "নাম প্রদান করা আবশ্যক")]
        [StringLength(100, ErrorMessage = "নাম ১০০ অক্ষরের মধ্যে হতে হবে")]
        [Display(Name = "নাম")]
        public string Name { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "মোবাইল নম্বর")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "সম্পর্ক নির্বাচন করুন")]
        [StringLength(50)]
        [Display(Name = "সম্পর্ক")]
        public string Relationship { get; set; } = "প্রধান ভাড়াটিয়া";

        [Display(Name = "প্রধান ভাড়াটিয়া")]
        public bool IsPrimaryTenant { get; set; } = false;

        [Display(Name = "অগ্রিম জমার পরিমাণ (টাকা)")]
        public decimal AdvanceAmount { get; set; } = 0.00m;

        [StringLength(100)]
        [Display(Name = "পেশা")]
        public string? Profession { get; set; }

        [StringLength(150)]
        [Display(Name = "প্রতিষ্ঠান / কর্মস্থল")]
        public string? Organization { get; set; }

        [StringLength(50)]
        [Display(Name = "জন্মতারিখ / বয়স")]
        public string? DateOfBirthOrAge { get; set; }

        [StringLength(255)]
        [Display(Name = "বর্তমান ঠিকানা")]
        public string? CurrentAddress { get; set; }

        [StringLength(255)]
        [Display(Name = "স্থায়ী ঠিকানা")]
        public string? PermanentAddress { get; set; }

        public bool IsChild { get; set; } = false;

        public IFormFile? PhotoFile { get; set; }
        public IFormFile? NidFrontFile { get; set; }
        public IFormFile? NidBackFile { get; set; }
    }
}
