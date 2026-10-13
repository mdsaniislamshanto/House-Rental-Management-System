using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shanto_VilaManagement_System.Data;
using Shanto_VilaManagement_System.Helpers;
using Shanto_VilaManagement_System.Services;
using Shanto_VilaManagement_System.ViewModels;

namespace Shanto_VilaManagement_System.Controllers
{
    public class PaymentsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IBillService _billService;

        public PaymentsController(AppDbContext context, IBillService billService)
        {
            _context = context;
            _billService = billService;
        }

        // GET: Payments
        public async Task<IActionResult> Index(int? year, int? month, int? roomId, string? status, string? searchTenant)
        {
            var query = _context.MonthlyBills.Include(b => b.Room).AsQueryable();

            if (year.HasValue && year.Value > 0)
            {
                query = query.Where(b => b.BillingYear == year.Value);
            }

            if (month.HasValue && month.Value > 0)
            {
                query = query.Where(b => b.BillingMonthNumber == month.Value);
            }

            if (roomId.HasValue && roomId.Value > 0)
            {
                query = query.Where(b => b.RoomId == roomId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query = query.Where(b => b.PaymentStatus == status);
            }

            if (!string.IsNullOrWhiteSpace(searchTenant))
            {
                query = query.Where(b => b.TenantName.Contains(searchTenant));
            }

            var bills = await query.ToListAsync();
            bills = bills
                .OrderByDescending(b => b.BillingYear)
                .ThenByDescending(b => b.BillingMonthNumber)
                .ThenBy(b => BanglaHelper.ExtractRoomNumberSortValue(b.Room?.RoomNumber))
                .ThenBy(b => b.Room?.RoomNumber)
                .ToList();

            var rooms = await _context.Rooms.ToListAsync();
            rooms = rooms
                .OrderBy(r => BanglaHelper.ExtractRoomNumberSortValue(r.RoomNumber))
                .ThenBy(r => r.RoomNumber)
                .ToList();

            var model = new PaymentFilterViewModel
            {
                Status = status ?? "All",
                Year = year,
                Month = month,
                RoomId = roomId,
                SearchTenant = searchTenant,
                Bills = bills,
                Rooms = rooms
            };

            return View(model);
        }

        // POST: Payments/MarkPaid
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkPaid(int billId, string? paymentType, decimal? paidAmount, DateTime? paidDate, string? returnUrl)
        {
            var date = paidDate ?? DateTime.Today;
            var type = string.Equals(paymentType, "Partial", StringComparison.OrdinalIgnoreCase) ? "Partial" : "Full";

            try
            {
                var success = await _billService.RecordPaymentAsync(billId, type, paidAmount, date);

                if (success)
                {
                    var updatedBill = await _context.MonthlyBills.FindAsync(billId);
                    if (type == "Partial" && updatedBill != null && updatedBill.PaymentStatus == "Partial")
                    {
                        TempData["SuccessMessage"] = $"বিলের আংশিক পেমেন্ট সফলভাবে সংরক্ষিত হয়েছে। জমা: {BanglaHelper.ToBanglaCurrency(updatedBill.PaidAmount)}, অবশিষ্ট বকেয়া: {BanglaHelper.ToBanglaCurrency(updatedBill.DueAmount)} (তারিখ: {BanglaHelper.FormatBanglaDate(date)})।";
                    }
                    else
                    {
                        TempData["SuccessMessage"] = $"বিলটি সম্পূর্ণ পরিশোধিত হিসেবে চিহ্নিত করা হয়েছে (তারিখ: {BanglaHelper.FormatBanglaDate(date)})।";
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "বিলটি খুঁজে পাওয়া যায়নি।";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Payments/MarkUnpaid
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkUnpaid(int billId, string? returnUrl)
        {
            var success = await _billService.MarkBillAsUnpaidAsync(billId);

            if (success)
            {
                TempData["SuccessMessage"] = "বিলটি বকেয়া হিসেবে চিহ্নিত করা হয়েছে।";
            }
            else
            {
                TempData["ErrorMessage"] = "বিলটি খুঁজে পাওয়া যায়নি।";
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Payments/RoomHistory/5
        public async Task<IActionResult> RoomHistory(int roomId)
        {
            var room = await _context.Rooms
                .Include(r => r.Residents)
                .FirstOrDefaultAsync(r => r.Id == roomId);

            if (room == null) return NotFound();

            var bills = await _context.MonthlyBills
                .Where(b => b.RoomId == roomId)
                .OrderByDescending(b => b.BillingYear)
                .ThenByDescending(b => b.BillingMonthNumber)
                .ToListAsync();

            ViewBag.Room = room;
            return View(bills);
        }
    }
}
