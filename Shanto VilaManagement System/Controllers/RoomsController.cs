using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shanto_VilaManagement_System.Data;
using Shanto_VilaManagement_System.Helpers;
using Shanto_VilaManagement_System.Models;

namespace Shanto_VilaManagement_System.Controllers
{
    public class RoomsController : Controller
    {
        private readonly AppDbContext _context;

        public RoomsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Rooms
        public async Task<IActionResult> Index()
        {
            var rooms = await _context.Rooms
                .Include(r => r.Residents)
                .ToListAsync();

            rooms = rooms
                .OrderBy(r => BanglaHelper.ExtractRoomNumberSortValue(r.RoomNumber))
                .ThenBy(r => r.RoomNumber)
                .ToList();

            return View(rooms);
        }

        // GET: Rooms/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var room = await _context.Rooms
                .Include(r => r.Residents)
                .Include(r => r.EmergencyContacts)
                .Include(r => r.MonthlyBills.OrderByDescending(b => b.BillingYear).ThenByDescending(b => b.BillingMonthNumber))
                .FirstOrDefaultAsync(m => m.Id == id);

            if (room == null) return NotFound();

            return View(room);
        }

        // GET: Rooms/Create
        public IActionResult Create()
        {
            return View(new Room { Status = "Vacant", RoomRent = 8000 });
        }

        // POST: Rooms/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RoomNumber,RoomRent,MeterNumber,Status,InitialMeterReading,OtherBill")] Room room)
        {
            if (await _context.Rooms.AnyAsync(r => r.RoomNumber == room.RoomNumber))
            {
                ModelState.AddModelError("RoomNumber", "এই রুম নম্বরটি ইতোমধ্যে সংরক্ষিত রয়েছে।");
            }

            if (ModelState.IsValid)
            {
                room.CreatedAt = DateTime.Now;
                _context.Add(room);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"রুম {room.RoomNumber} সফলভাবে তৈরি করা হয়েছে।";
                return RedirectToAction(nameof(Index));
            }
            return View(room);
        }

        // GET: Rooms/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();

            return View(room);
        }

        // POST: Rooms/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,RoomNumber,RoomRent,MeterNumber,Status,InitialMeterReading,CreatedAt,OtherBill")] Room room)
        {
            if (id != room.Id) return NotFound();

            if (await _context.Rooms.AnyAsync(r => r.RoomNumber == room.RoomNumber && r.Id != room.Id))
            {
                ModelState.AddModelError("RoomNumber", "এই রুম নম্বরটি অন্য রুমে ব্যবহৃত হচ্ছে।");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(room);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"রুম {room.RoomNumber}-এর তথ্য সফলভাবে আপডেট করা হয়েছে। পুরোনো বিল অপরিবর্তিত থাকবে।";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Rooms.AnyAsync(e => e.Id == room.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(room);
        }

        // POST: Rooms/SetStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetStatus(int id, string status)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();

            room.Status = status;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"রুম {room.RoomNumber}-এর অবস্থা পরিবর্তন করা হয়েছে।";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Rooms/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var room = await _context.Rooms
                .Include(r => r.Residents)
                .Include(r => r.MonthlyBills)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (room == null) return NotFound();

            return View(room);
        }

        // POST: Rooms/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, bool forceDelete = false)
        {
            var room = await _context.Rooms
                .Include(r => r.MonthlyBills)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null) return NotFound();

            if (room.MonthlyBills.Any() && !forceDelete)
            {
                TempData["ErrorMessage"] = "এই রুমের পূর্বে সংরক্ষিত মাসিক বিলের রেকর্ড রয়েছে। ঐতিহাসিক তথ্য সংরক্ষণের জন্য রুমটি Permanent Delete না করে 'নিষ্ক্রিয় (Inactive)' বা 'খালি (Vacant)' করার পরামর্শ দেওয়া হচ্ছে।";
                return RedirectToAction(nameof(Delete), new { id });
            }

            if (room.MonthlyBills.Any() && forceDelete)
            {
                _context.MonthlyBills.RemoveRange(room.MonthlyBills);
            }

            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"রুম {room.RoomNumber} সফলভাবে ডিলিট করা হয়েছে।";
            return RedirectToAction(nameof(Index));
        }
    }
}
