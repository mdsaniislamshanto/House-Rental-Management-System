using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shanto_VilaManagement_System.Data;
using Shanto_VilaManagement_System.Helpers;
using Shanto_VilaManagement_System.Models;
using Shanto_VilaManagement_System.Services;

namespace Shanto_VilaManagement_System.Controllers
{
    public class BillsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IPdfService _pdfService;

        public BillsController(AppDbContext context, IPdfService pdfService)
        {
            _context = context;
            _pdfService = pdfService;
        }

        // GET: Bills
        public async Task<IActionResult> Index(int? year, int? month, int? roomId, string? status)
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

            var bills = await query.ToListAsync();
            bills = bills
                .OrderByDescending(b => b.BillingYear)
                .ThenByDescending(b => b.BillingMonthNumber)
                .ThenBy(b => BanglaHelper.ExtractRoomNumberSortValue(b.Room?.RoomNumber))
                .ThenBy(b => b.Room?.RoomNumber)
                .ToList();

            var rooms = await _context.Rooms.ToListAsync();
            ViewBag.Rooms = rooms
                .OrderBy(r => BanglaHelper.ExtractRoomNumberSortValue(r.RoomNumber))
                .ThenBy(r => r.RoomNumber)
                .ToList();
            ViewBag.SelectedYear = year;
            ViewBag.SelectedMonth = month;
            ViewBag.SelectedRoomId = roomId;
            ViewBag.SelectedStatus = status;

            return View(bills);
        }

        // GET: Bills/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var bill = await _context.MonthlyBills
                .Include(b => b.Room)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (bill == null) return NotFound();

            var setting = await _context.Settings.FirstOrDefaultAsync() ?? new Setting();
            ViewBag.Setting = setting;

            return View(bill);
        }

        // GET: Bills/Print/5 (Printable Web View)
        public async Task<IActionResult> Print(int? id)
        {
            if (id == null) return NotFound();

            var bill = await _context.MonthlyBills
                .Include(b => b.Room)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (bill == null) return NotFound();

            var setting = await _context.Settings.FirstOrDefaultAsync() ?? new Setting();
            ViewBag.Setting = setting;

            return View(bill);
        }

        // GET: Bills/DownloadPdf/5 (Bangla Unicode PDF Download)
        public async Task<IActionResult> DownloadPdf(int? id)
        {
            if (id == null) return NotFound();

            var bill = await _context.MonthlyBills
                .Include(b => b.Room)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (bill == null) return NotFound();

            var setting = await _context.Settings.FirstOrDefaultAsync() ?? new Setting();
            var pdfBytes = _pdfService.GenerateBanglaBillPdf(bill, setting);

            var fileName = $"Bill_Room_{bill.Room?.RoomNumber}_{bill.BillingYear}_{bill.BillingMonthNumber:D2}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        // GET: Bills/DownloadAllPdf (Batch A4 PDF with 6 bills per page)
        public async Task<IActionResult> DownloadAllPdf(int? year, int? month, int? roomId, string? status)
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

            var bills = await query.ToListAsync();
            bills = bills
                .OrderByDescending(b => b.BillingYear)
                .ThenByDescending(b => b.BillingMonthNumber)
                .ThenBy(b => BanglaHelper.ExtractRoomNumberSortValue(b.Room?.RoomNumber))
                .ThenBy(b => b.Room?.RoomNumber)
                .ToList();

            if (!bills.Any())
            {
                TempData["ErrorMessage"] = "ডাউনলোড করার মতো কোনো বিল পাওয়া যায়নি।";
                return RedirectToAction(nameof(Index), new { year, month, roomId, status });
            }

            var setting = await _context.Settings.FirstOrDefaultAsync() ?? new Setting();
            var pdfBytes = _pdfService.GenerateBatchA4BillsPdf(bills, setting);

            var monthSuffix = month.HasValue && month.Value > 0 ? $"_{month.Value:D2}" : "";
            var yearSuffix = year.HasValue && year.Value > 0 ? $"_{year.Value}" : "";
            var fileName = $"Monthly_Bills_A4_6PerSheet{yearSuffix}{monthSuffix}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }

        // POST: Bills/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var bill = await _context.MonthlyBills.FindAsync(id);
            if (bill != null)
            {
                _context.MonthlyBills.Remove(bill);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "মাসিক বিল সফলভাবে মুছে ফেলা হয়েছে।";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
