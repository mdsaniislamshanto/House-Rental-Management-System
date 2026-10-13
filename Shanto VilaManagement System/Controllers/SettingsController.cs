using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shanto_VilaManagement_System.Data;
using Shanto_VilaManagement_System.Models;
using Shanto_VilaManagement_System.Services;

using Shanto_VilaManagement_System.Helpers;

namespace Shanto_VilaManagement_System.Controllers
{
    public class SettingsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IDatabaseBackupService _backupService;
        private readonly ICloudStorageService _cloudStorage;

        public SettingsController(AppDbContext context, IDatabaseBackupService backupService, ICloudStorageService cloudStorage)
        {
            _context = context;
            _backupService = backupService;
            _cloudStorage = cloudStorage;
        }

        // GET: Settings
        public async Task<IActionResult> Index()
        {
            var setting = await _context.Settings.FirstOrDefaultAsync();
            if (setting == null)
            {
                setting = new Setting
                {
                    HouseName = "শান্ত ভিলা",
                    HouseAddress = "টেকপাড়া, কুনিয়া পাছর উত্তর, গাজীপুর সদর, গাজীপুর।",
                    CurrentUnitPrice = 12.00m,
                    DefaultOtherBill = 0m,
                    UpdatedAt = DateTime.Now
                };
                _context.Settings.Add(setting);
                await _context.SaveChangesAsync();
            }

            var rooms = await _context.Rooms
                .Include(r => r.Residents)
                .ToListAsync();

            rooms = rooms
                .OrderBy(r => BanglaHelper.ExtractRoomNumberSortValue(r.RoomNumber))
                .ThenBy(r => r.RoomNumber)
                .ToList();

            ViewBag.Rooms = rooms;
            ViewBag.IsCloudinaryConfigured = _cloudStorage.IsConfigured;
            return View(setting);
        }

        // POST: Settings/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            [Bind("Id,HouseName,HouseAddress,CurrentUnitPrice,DefaultOtherBill")] Setting setting,
            Dictionary<int, decimal?>? roomOtherBills = null)
        {
            if (ModelState.IsValid)
            {
                var existingSetting = await _context.Settings.FindAsync(setting.Id);
                if (existingSetting != null)
                {
                    existingSetting.HouseName = setting.HouseName;
                    existingSetting.HouseAddress = setting.HouseAddress;
                    existingSetting.CurrentUnitPrice = setting.CurrentUnitPrice;
                    existingSetting.DefaultOtherBill = setting.DefaultOtherBill;
                    existingSetting.UpdatedAt = DateTime.Now;
                }
                else
                {
                    setting.UpdatedAt = DateTime.Now;
                    _context.Add(setting);
                }

                if (roomOtherBills != null && roomOtherBills.Any())
                {
                    var roomIds = roomOtherBills.Keys.ToList();
                    var rooms = await _context.Rooms.Where(r => roomIds.Contains(r.Id)).ToListAsync();
                    foreach (var r in rooms)
                    {
                        if (roomOtherBills.TryGetValue(r.Id, out var customAmount))
                        {
                            r.OtherBill = customAmount;
                        }
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "সেটিংস ও অন্যান্য বিল সফলভাবে আপডেট করা হয়েছে।";
                return RedirectToAction(nameof(Index));
            }

            var allRooms = await _context.Rooms
                .Include(r => r.Residents)
                .ToListAsync();

            allRooms = allRooms
                .OrderBy(r => BanglaHelper.ExtractRoomNumberSortValue(r.RoomNumber))
                .ThenBy(r => r.RoomNumber)
                .ToList();

            ViewBag.Rooms = allRooms;
            ViewBag.IsCloudinaryConfigured = _cloudStorage.IsConfigured;
            return View("Index", setting);
        }

        // GET: Settings/DownloadBackup
        public async Task<IActionResult> DownloadBackup()
        {
            try
            {
                var backupBytes = await _backupService.GenerateSqlBackupAsync();
                var fileName = $"shanto_vila_db_backup_{DateTime.Now:yyyyMMdd_HHmmss}.sql";
                return File(backupBytes, "application/sql", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"ডাটাবেজ ব্যাকআপ তৈরি করতে সমস্যা হয়েছে: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
