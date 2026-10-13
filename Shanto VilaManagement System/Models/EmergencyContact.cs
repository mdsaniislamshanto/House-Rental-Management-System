using System.ComponentModel.DataAnnotations;

namespace Shanto_VilaManagement_System.Models
{
    public class EmergencyContact
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "রুম নির্বাচন করা আবশ্যক")]
        [Display(Name = "রুম")]
        public int RoomId { get; set; }

        public virtual Room? Room { get; set; }

        [Required(ErrorMessage = "যোগাযোগকারী ব্যক্তির নাম আবশ্যক")]
        [Display(Name = "নাম")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "সম্পর্ক প্রদান আবশ্যক")]
        [Display(Name = "সম্পর্ক")]
        [StringLength(50)]
        public string Relationship { get; set; } = string.Empty;

        [Required(ErrorMessage = "মোবাইল নম্বর প্রদান আবশ্যক")]
        [Display(Name = "মোবাইল নম্বর")]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "ঠিকানা")]
        [StringLength(255)]
        public string? Address { get; set; }
    }
}
