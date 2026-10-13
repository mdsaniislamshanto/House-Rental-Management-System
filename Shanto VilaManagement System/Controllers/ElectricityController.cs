using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shanto_VilaManagement_System.Data;
using Shanto_VilaManagement_System.Helpers;
using Shanto_VilaManagement_System.Services;
using Shanto_VilaManagement_System.ViewModels;

namespace Shanto_VilaManagement_System.Controllers
{
    public class ElectricityController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IBillService _billService;

        public ElectricityController(AppDbContext context, IBillService billService)
        {
            _context = context;
            _billService = billService;
        }

        // GET: Electricity
        public async Task<IActionResult> Index(int? year, int? month)
        {
            var defaultPeriod = BanglaHelper.GetDefaultBillingPeriod();
            var targetYear = year ?? defaultPeriod.Year;
            var targetMonth = month ?? defaultPeriod.Month;

            var model = await _billService.PrepareBulkReadingViewModelAsync(targetYear, targetMonth);
            return View(model);
        }

        // POST: Electricity/SaveBulkReadings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveBulkReadings(BulkMeterReadingViewModel model)
        {
            var result = await _billService.ProcessBulkReadingAsync(model);

            if (result.SuccessCount > 0)
            {
                TempData["SuccessMessage"] = $"{BanglaHelper.ToBanglaDigits(result.SuccessCount)} টি রুমের বিদ্যুৎ রিডিং ও মাসিক বিল সফলভাবে সংরক্ষিত এবং প্রস্তুত করা হয়েছে।";
            }

            if (result.Errors.Any())
            {
                TempData["ErrorMessage"] = string.Join("<br/>", result.Errors);
            }

            return RedirectToAction(nameof(Index), new { year = model.Year, month = model.Month });
        }

        // GET: Electricity/SingleEntry
        public async Task<IActionResult> SingleEntry(int roomId, int? year, int? month)
        {
            var defaultPeriod = BanglaHelper.GetDefaultBillingPeriod();
            var targetYear = year ?? defaultPeriod.Year;
            var targetMonth = month ?? defaultPeriod.Month;

            var room = await _context.Rooms
                .Include(r => r.Residents)
                .FirstOrDefaultAsync(r => r.Id == roomId);

            if (room == null) return NotFound();

            var setting = await _context.Settings.FirstOrDefaultAsync();
            var prevReading = await _billService.GetPreviousReadingAsync(roomId, targetYear, targetMonth);

            var existingBill = await _context.MonthlyBills
                .FirstOrDefaultAsync(b => b.RoomId == roomId && b.BillingYear == targetYear && b.BillingMonthNumber == targetMonth);

            var prevDue = existingBill?.PreviousDue ?? await _billService.GetPreviousDueAsync(roomId, targetYear, targetMonth);
            var otherBill = existingBill?.OtherBill ?? (setting?.GetOtherBillForRoom(room) ?? 0m);

            ViewBag.Room = room;
            ViewBag.Year = targetYear;
            ViewBag.Month = targetMonth;
            ViewBag.UnitPrice = setting?.CurrentUnitPrice ?? 12.00m;
            ViewBag.OtherBill = otherBill;
            ViewBag.PreviousReading = prevReading;
            ViewBag.PreviousDue = prevDue;
            ViewBag.CurrentReading = existingBill?.CurrentReading;
            ViewBag.ExistingBill = existingBill;

            return View();
        }

        // POST: Electricity/SaveSingleEntry
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSingleEntry(int roomId, int year, int month, decimal currentReading, decimal previousReading, decimal? previousDue = null, bool isEdit = false)
        {
            var existingBill = await _context.MonthlyBills
                .FirstOrDefaultAsync(b => b.RoomId == roomId && b.BillingYear == year && b.BillingMonthNumber == month);

            if (existingBill != null && !isEdit)
            {
                TempData["ErrorMessage"] = $"রুমের {BanglaHelper.FormatBillingMonthBangla(year, month)} মাসের বর্তমান রিডিং ইতিমধ্যে একবার ইনপুট করা হয়েছে। একই মাসে একাধিকবার নতুন ইনপুট দেওয়া যাবে না। কোনো সংশোধনের প্রয়োজন হলে 'এডিট' অপশন ব্যবহার করুন।";
                return RedirectToAction(nameof(SingleEntry), new { roomId, year, month });
            }

            if (currentReading < previousReading)
            {
                TempData["ErrorMessage"] = "বর্তমান মিটার রিডিং পূর্বের মিটার রিডিংয়ের চেয়ে কম হতে পারবে না।";
                return RedirectToAction(nameof(SingleEntry), new { roomId, year, month });
            }

            try
            {
                var bill = await _billService.GenerateOrUpdateBillAsync(roomId, year, month, currentReading, previousReading, previousDue);
                if (existingBill != null && isEdit)
                {
                    TempData["SuccessMessage"] = $"রুমের {BanglaHelper.FormatBillingMonthBangla(year, month)} মাসের মিটার রিডিং সফলভাবে এডিট ও আপডেট করা হয়েছে। মোট বিল: {BanglaHelper.ToBanglaCurrency(bill.TotalBill)}";
                }
                else
                {
                    TempData["SuccessMessage"] = $"রুমের মাসিক বিল সফলভাবে তৈরি করা হয়েছে। মোট বিল: {BanglaHelper.ToBanglaCurrency(bill.TotalBill)}";
                }
                return RedirectToAction("Details", "Bills", new { id = bill.Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(SingleEntry), new { roomId, year, month });
            }
        }

        // GET: Electricity/History
        public async Task<IActionResult> History(int? roomId)
        {
            var query = _context.MonthlyBills.Include(b => b.Room).AsQueryable();

            if (roomId.HasValue)
            {
                query = query.Where(b => b.RoomId == roomId.Value);
                ViewBag.SelectedRoom = await _context.Rooms.FindAsync(roomId.Value);
            }

            var rooms = await _context.Rooms.ToListAsync();
            ViewBag.Rooms = rooms
                .OrderBy(r => BanglaHelper.ExtractRoomNumberSortValue(r.RoomNumber))
                .ThenBy(r => r.RoomNumber)
                .ToList();

            var readings = await query
                .OrderByDescending(b => b.BillingYear)
                .ThenByDescending(b => b.BillingMonthNumber)
                .ToListAsync();

            return View(readings);
        }
    }
}
