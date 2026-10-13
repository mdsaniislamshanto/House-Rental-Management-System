using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shanto_VilaManagement_System.Data;
using Shanto_VilaManagement_System.Helpers;
using Shanto_VilaManagement_System.ViewModels;

namespace Shanto_VilaManagement_System.Controllers
{
    public class SearchController : Controller
    {
        private readonly AppDbContext _context;

        public SearchController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Search?q=...
        public async Task<IActionResult> Index(string? q)
        {
            var model = new SearchViewModel
            {
                Query = q
            };

            if (string.IsNullOrWhiteSpace(q))
            {
                return View(model);
            }

            var queryClean = q.Trim();
            var banglaDigits = BanglaHelper.ToBanglaDigits(queryClean);

            // 1. Search Rooms
            var matchedRooms = await _context.Rooms
                .Include(r => r.Residents)
                .Where(r => r.RoomNumber.Contains(queryClean) || 
                            r.RoomNumber.Contains(banglaDigits) ||
                            (r.MeterNumber != null && r.MeterNumber.Contains(queryClean)))
                .ToListAsync();

            model.MatchedRooms = matchedRooms
                .OrderBy(r => BanglaHelper.ExtractRoomNumberSortValue(r.RoomNumber))
                .ThenBy(r => r.RoomNumber)
                .ToList();

            // 2. Search Residents
            model.MatchedResidents = await _context.Residents
                .Include(r => r.Room)
                .Where(r => r.Name.Contains(queryClean) ||
                            r.Phone.Contains(queryClean) ||
                            r.Phone.Contains(banglaDigits) ||
                            (r.Profession != null && r.Profession.Contains(queryClean)) ||
                            (r.Organization != null && r.Organization.Contains(queryClean)) ||
                            (r.CurrentAddress != null && r.CurrentAddress.Contains(queryClean)) ||
                            (r.PermanentAddress != null && r.PermanentAddress.Contains(queryClean)))
                .ToListAsync();

            // 3. Search Monthly Bills
            var matchedBills = await _context.MonthlyBills
                .Include(b => b.Room)
                .Where(b => b.TenantName.Contains(queryClean) ||
                            b.Room!.RoomNumber.Contains(queryClean) ||
                            b.Room!.RoomNumber.Contains(banglaDigits) ||
                            b.BillingMonth.Contains(queryClean))
                .ToListAsync();

            model.MatchedBills = matchedBills
                .OrderByDescending(b => b.BillingYear)
                .ThenByDescending(b => b.BillingMonthNumber)
                .ThenBy(b => BanglaHelper.ExtractRoomNumberSortValue(b.Room?.RoomNumber))
                .ThenBy(b => b.Room?.RoomNumber)
                .ToList();

            return View(model);
        }
    }
}
