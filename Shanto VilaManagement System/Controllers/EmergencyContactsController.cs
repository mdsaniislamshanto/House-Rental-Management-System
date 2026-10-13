using Microsoft.AspNetCore.Mvc;
using Shanto_VilaManagement_System.Data;
using Shanto_VilaManagement_System.Models;

namespace Shanto_VilaManagement_System.Controllers
{
    public class EmergencyContactsController : Controller
    {
        private readonly AppDbContext _context;

        public EmergencyContactsController(AppDbContext context)
        {
            _context = context;
        }

        // POST: EmergencyContacts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RoomId,Name,Relationship,Phone,Address")] EmergencyContact contact)
        {
            if (ModelState.IsValid)
            {
                _context.EmergencyContacts.Add(contact);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "জরুরী যোগাযোগের তথ্য যুক্ত করা হয়েছে।";
            }
            else
            {
                TempData["ErrorMessage"] = "জরুরী যোগাযোগের তথ্য সঠিকভাবে পূরণ করুন।";
            }

            return RedirectToAction("Details", "Rooms", new { id = contact.RoomId });
        }

        // POST: EmergencyContacts/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int roomId)
        {
            var contact = await _context.EmergencyContacts.FindAsync(id);
            if (contact != null)
            {
                _context.EmergencyContacts.Remove(contact);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "জরুরী যোগাযোগের তথ্য মুছে ফেলা হয়েছে।";
            }

            return RedirectToAction("Details", "Rooms", new { id = roomId });
        }
    }
}
