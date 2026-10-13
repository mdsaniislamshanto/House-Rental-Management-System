using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shanto_VilaManagement_System.Models
{
    public class Setting
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "বাড়ির নাম আবশ্যক")]
        [Display(Name = "বাড়ির নাম")]
        [StringLength(100)]
        public string HouseName { get; set; } = "শান্ত ভিলা";

        [Display(Name = "বাড়ির ঠিকানা")]
        [StringLength(255)]
        public string HouseAddress { get; set; } = "টেকপাড়া, কুনিয়া পাছর উত্তর, গাজীপুর সদর, গাজীপুর।";

        [Required(ErrorMessage = "বর্তমান ইউনিট মূল্য আবশ্যক")]
        [Display(Name = "বর্তমান ইউনিট মূল্য (টাকা)")]
        [Column(TypeName = "decimal(10, 2)")]
        [Range(0.01, 1000, ErrorMessage = "ইউনিট মূল্য ০ এর বেশি হতে হবে")]
        public decimal CurrentUnitPrice { get; set; } = 12.00m;

        [Display(Name = "সকল রুমের সাধারণ অন্যান্য বিল (টাকা)")]
        [Column(TypeName = "decimal(12, 2)")]
        [Range(0, 100000, ErrorMessage = "টাকার পরিমাণ ০ বা তার বেশি হতে হবে")]
        public decimal DefaultOtherBill { get; set; } = 0m;

        [Column(TypeName = "decimal(12, 2)")]
        public decimal OtherBill1To31 { get; set; } = 0m;

        [Column(TypeName = "decimal(12, 2)")]
        public decimal OtherBill32To42 { get; set; } = 0m;

        [Display(Name = "সর্বশেষ আপডেট")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public decimal GetOtherBillForRoom(Room? room)
        {
            if (room?.OtherBill.HasValue == true)
            {
                return room.OtherBill.Value;
            }
            return DefaultOtherBill;
        }

        public decimal GetOtherBillForRoom(string? roomNumber)
        {
            return DefaultOtherBill;
        }
    }
}
