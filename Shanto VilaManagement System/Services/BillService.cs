using Microsoft.EntityFrameworkCore;
using Shanto_VilaManagement_System.Data;
using Shanto_VilaManagement_System.Helpers;
using Shanto_VilaManagement_System.Models;
using Shanto_VilaManagement_System.ViewModels;

namespace Shanto_VilaManagement_System.Services
{
    public class BillService : IBillService
    {
        private readonly AppDbContext _context;

        public BillService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<decimal> GetPreviousReadingAsync(int roomId, int targetYear, int targetMonth)
        {
            // Find the most recent bill before the target month for this room
            var lastBill = await _context.MonthlyBills
                .Where(b => b.RoomId == roomId &&
                           (b.BillingYear < targetYear || (b.BillingYear == targetYear && b.BillingMonthNumber < targetMonth)))
                .OrderByDescending(b => b.BillingYear)
                .ThenByDescending(b => b.BillingMonthNumber)
                .FirstOrDefaultAsync();

            if (lastBill != null)
            {
                return lastBill.CurrentReading;
            }

            // Alternatively, find any latest bill if target is new
            var anyLastBill = await _context.MonthlyBills
                .Where(b => b.RoomId == roomId)
                .OrderByDescending(b => b.BillingYear)
                .ThenByDescending(b => b.BillingMonthNumber)
                .FirstOrDefaultAsync();

            if (anyLastBill != null)
            {
                return anyLastBill.CurrentReading;
            }

            // If no previous bill exists, return the room's initial meter reading
            var room = await _context.Rooms.FindAsync(roomId);
            return room?.InitialMeterReading ?? 0m;
        }

        public async Task<decimal> GetPreviousDueAsync(int roomId, int targetYear, int targetMonth)
        {
            // Find the bill immediately preceding the target month for this room
            var lastBill = await _context.MonthlyBills
                .Where(b => b.RoomId == roomId &&
                           (b.BillingYear < targetYear || (b.BillingYear == targetYear && b.BillingMonthNumber < targetMonth)))
                .OrderByDescending(b => b.BillingYear)
                .ThenByDescending(b => b.BillingMonthNumber)
                .FirstOrDefaultAsync();

            if (lastBill == null) return 0m;

            if (lastBill.PaymentStatus == "Paid")
            {
                return 0m;
            }

            return lastBill.DueAmount > 0 ? lastBill.DueAmount : (lastBill.TotalBill - lastBill.PaidAmount);
        }

        public async Task<MonthlyBill> GenerateOrUpdateBillAsync(int roomId, int year, int month, decimal currentReading, decimal? customPreviousReading = null, decimal? manualPreviousDue = null)
        {
            var room = await _context.Rooms
                .Include(r => r.Residents)
                .FirstOrDefaultAsync(r => r.Id == roomId)
                ?? throw new KeyNotFoundException($"রুম আইডি {roomId} পাওয়া যায়নি।");

            var previousReading = customPreviousReading ?? await GetPreviousReadingAsync(roomId, year, month);

            if (currentReading < previousReading)
            {
                throw new InvalidOperationException("বর্তমান মিটার রিডিং পূর্বের মিটার রিডিংয়ের চেয়ে কম হতে পারবে না।");
            }

            var setting = await _context.Settings.FirstOrDefaultAsync() ?? new Setting();
            var unitPrice = setting.CurrentUnitPrice;

            var usedUnit = currentReading - previousReading;
            var electricityBill = usedUnit * unitPrice;
            var roomRent = room.RoomRent;
            var otherBill = setting.GetOtherBillForRoom(room);
            var previousDue = manualPreviousDue ?? await GetPreviousDueAsync(roomId, year, month);
            var totalBill = roomRent + electricityBill + otherBill + previousDue;
            var tenantName = room.PrimaryTenant?.Name ?? (room.Residents.FirstOrDefault()?.Name ?? "অনির্ধারিত");
            var monthDisplay = BanglaHelper.FormatBillingMonthBangla(year, month);

            var existingBill = await _context.MonthlyBills
                .FirstOrDefaultAsync(b => b.RoomId == roomId && b.BillingYear == year && b.BillingMonthNumber == month);

            if (existingBill != null)
            {
                existingBill.TenantName = tenantName;
                existingBill.RoomRent = roomRent;
                existingBill.PreviousReading = previousReading;
                existingBill.CurrentReading = currentReading;
                existingBill.UsedUnit = usedUnit;
                existingBill.UnitPrice = unitPrice;
                existingBill.ElectricityBill = electricityBill;
                existingBill.OtherBill = otherBill;
                existingBill.PreviousDue = previousDue;
                existingBill.TotalBill = totalBill;
                existingBill.BillingMonth = monthDisplay;

                if (existingBill.PaymentStatus == "Paid")
                {
                    existingBill.PaidAmount = totalBill;
                    existingBill.DueAmount = 0m;
                }
                else if (existingBill.PaymentStatus == "Partial")
                {
                    existingBill.DueAmount = Math.Max(0m, totalBill - existingBill.PaidAmount);
                    if (existingBill.DueAmount == 0m)
                    {
                        existingBill.PaymentStatus = "Paid";
                    }
                }
                else
                {
                    existingBill.PaidAmount = 0m;
                    existingBill.DueAmount = totalBill;
                }

                await _context.SaveChangesAsync();
                return existingBill;
            }
            else
            {
                var newBill = new MonthlyBill
                {
                    RoomId = roomId,
                    BillingYear = year,
                    BillingMonthNumber = month,
                    BillingMonth = monthDisplay,
                    TenantName = tenantName,
                    RoomRent = roomRent,
                    PreviousReading = previousReading,
                    CurrentReading = currentReading,
                    UsedUnit = usedUnit,
                    UnitPrice = unitPrice,
                    ElectricityBill = electricityBill,
                    OtherBill = otherBill,
                    PreviousDue = previousDue,
                    TotalBill = totalBill,
                    PaymentStatus = "Unpaid",
                    PaidAmount = 0m,
                    DueAmount = totalBill,
                    PaidDate = null,
                    CreatedAt = DateTime.Now
                };

                _context.MonthlyBills.Add(newBill);
                try
                {
                    await _context.SaveChangesAsync();
                    return newBill;
                }
                catch (DbUpdateException)
                {
                    // Concurrency race condition: if another thread already inserted the bill, update it instead
                    _context.ChangeTracker.Clear();
                    var concurrentBill = await _context.MonthlyBills
                        .FirstOrDefaultAsync(b => b.RoomId == roomId && b.BillingYear == year && b.BillingMonthNumber == month);
                    if (concurrentBill != null)
                    {
                        concurrentBill.TenantName = tenantName;
                        concurrentBill.RoomRent = roomRent;
                        concurrentBill.PreviousReading = previousReading;
                        concurrentBill.CurrentReading = currentReading;
                        concurrentBill.UsedUnit = usedUnit;
                        concurrentBill.UnitPrice = unitPrice;
                        concurrentBill.ElectricityBill = electricityBill;
                        concurrentBill.OtherBill = otherBill;
                        concurrentBill.PreviousDue = previousDue;
                        concurrentBill.TotalBill = totalBill;
                        concurrentBill.BillingMonth = monthDisplay;
                        await _context.SaveChangesAsync();
                        return concurrentBill;
                    }
                    throw;
                }
            }
        }

        public async Task<bool> RecordPaymentAsync(int billId, string paymentType, decimal? customAmount, DateTime paidDate)
        {
            var bill = await _context.MonthlyBills.FindAsync(billId);
            if (bill == null) return false;

            if (paymentType == "Partial")
            {
                var amount = customAmount ?? 0m;
                if (amount <= 0m)
                {
                    throw new InvalidOperationException("আংশিক পরিশোধের ক্ষেত্রে টাকার পরিমাণ শূন্যের চেয়ে বেশি হতে হবে।");
                }

                if (amount >= bill.TotalBill)
                {
                    bill.PaymentStatus = "Paid";
                    bill.PaidAmount = bill.TotalBill;
                    bill.DueAmount = 0m;
                    bill.PaidDate = paidDate;
                }
                else
                {
                    bill.PaymentStatus = "Partial";
                    bill.PaidAmount = amount;
                    bill.DueAmount = bill.TotalBill - amount;
                    bill.PaidDate = paidDate;
                }
            }
            else // Full payment
            {
                bill.PaymentStatus = "Paid";
                bill.PaidAmount = bill.TotalBill;
                bill.DueAmount = 0m;
                bill.PaidDate = paidDate;
            }

            // Sync subsequent bill for this room if one already exists
            await SyncSubsequentBillDueAsync(bill);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkBillAsPaidAsync(int billId, DateTime paidDate)
        {
            return await RecordPaymentAsync(billId, "Full", null, paidDate);
        }

        public async Task<bool> MarkBillAsUnpaidAsync(int billId)
        {
            var bill = await _context.MonthlyBills.FindAsync(billId);
            if (bill == null) return false;

            bill.PaymentStatus = "Unpaid";
            bill.PaidAmount = 0m;
            bill.DueAmount = bill.TotalBill;
            bill.PaidDate = null;

            await SyncSubsequentBillDueAsync(bill);

            await _context.SaveChangesAsync();
            return true;
        }

        private async Task SyncSubsequentBillDueAsync(MonthlyBill bill)
        {
            var nextBill = await _context.MonthlyBills
                .Where(b => b.RoomId == bill.RoomId &&
                           (b.BillingYear > bill.BillingYear || (b.BillingYear == bill.BillingYear && b.BillingMonthNumber > bill.BillingMonthNumber)))
                .OrderBy(b => b.BillingYear)
                .ThenBy(b => b.BillingMonthNumber)
                .FirstOrDefaultAsync();

            if (nextBill != null)
            {
                nextBill.PreviousDue = bill.DueAmount;
                nextBill.TotalBill = nextBill.RoomRent + nextBill.ElectricityBill + nextBill.OtherBill + nextBill.PreviousDue;
                if (nextBill.PaymentStatus == "Paid")
                {
                    nextBill.PaidAmount = nextBill.TotalBill;
                    nextBill.DueAmount = 0m;
                }
                else if (nextBill.PaymentStatus == "Partial")
                {
                    nextBill.DueAmount = Math.Max(0m, nextBill.TotalBill - nextBill.PaidAmount);
                    if (nextBill.DueAmount == 0m)
                    {
                        nextBill.PaymentStatus = "Paid";
                    }
                }
                else
                {
                    nextBill.PaidAmount = 0m;
                    nextBill.DueAmount = nextBill.TotalBill;
                }
            }
        }

        public async Task<BulkMeterReadingViewModel> PrepareBulkReadingViewModelAsync(int year, int month)
        {
            var setting = await _context.Settings.FirstOrDefaultAsync() ?? new Setting();

            var rooms = await _context.Rooms
                .Include(r => r.Residents)
                .Where(r => r.Status != "Inactive")
                .ToListAsync();

            rooms = rooms
                .OrderBy(r => BanglaHelper.ExtractRoomNumberSortValue(r.RoomNumber))
                .ThenBy(r => r.RoomNumber)
                .ToList();

            var existingBills = await _context.MonthlyBills
                .Where(b => b.BillingYear == year && b.BillingMonthNumber == month)
                .ToListAsync();

            var model = new BulkMeterReadingViewModel
            {
                Year = year,
                Month = month,
                UnitPrice = setting.CurrentUnitPrice,
                Items = new List<RoomReadingItemViewModel>()
            };

            foreach (var room in rooms)
            {
                var existing = existingBills.FirstOrDefault(b => b.RoomId == room.Id);
                decimal previous = existing?.PreviousReading ?? await GetPreviousReadingAsync(room.Id, year, month);
                decimal previousDue = existing?.PreviousDue ?? await GetPreviousDueAsync(room.Id, year, month);
                decimal otherBill = existing?.OtherBill ?? setting.GetOtherBillForRoom(room);

                model.Items.Add(new RoomReadingItemViewModel
                {
                    RoomId = room.Id,
                    RoomNumber = room.RoomNumber,
                    MeterNumber = room.MeterNumber,
                    TenantName = room.PrimaryTenant?.Name ?? (room.Residents.FirstOrDefault()?.Name ?? "খালি / অনির্ধারিত"),
                    RoomRent = room.RoomRent,
                    OtherBill = otherBill,
                    PreviousReading = previous,
                    PreviousDue = previousDue,
                    CurrentReading = existing?.CurrentReading,
                    HasExistingBill = existing != null,
                    ExistingBillId = existing?.Id,
                    ExistingPaymentStatus = existing?.PaymentStatus
                });
            }

            return model;
        }

        public async Task<(int SuccessCount, List<string> Errors)> ProcessBulkReadingAsync(BulkMeterReadingViewModel model)
        {
            var errors = new List<string>();
            int successCount = 0;

            foreach (var item in model.Items)
            {
                if (!item.CurrentReading.HasValue)
                {
                    // Skipped if no current reading entered
                    continue;
                }

                // Check if bill already exists for this room in this month
                var existingBill = await _context.MonthlyBills
                    .FirstOrDefaultAsync(b => b.RoomId == item.RoomId && b.BillingYear == model.Year && b.BillingMonthNumber == model.Month);

                if (existingBill != null && !item.IsEditMode)
                {
                    // Reading was already inputted for this month and was not marked for edit; skip to prevent duplicate input
                    continue;
                }

                if (item.CurrentReading.Value < item.PreviousReading)
                {
                    errors.Add($"রুম {item.RoomNumber}: বর্তমান রিডিং ({BanglaHelper.ToBanglaDigits(item.CurrentReading.Value)}) পূর্বের রিডিং ({BanglaHelper.ToBanglaDigits(item.PreviousReading)})-এর চেয়ে কম হতে পারবে না।");
                    continue;
                }

                try
                {
                    await GenerateOrUpdateBillAsync(item.RoomId, model.Year, model.Month, item.CurrentReading.Value, item.PreviousReading, item.PreviousDue);
                    successCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"রুম {item.RoomNumber}: {ex.Message}");
                }
            }

            return (successCount, errors);
        }

        public async Task<DashboardViewModel> GetDashboardStatsAsync(int? year = null, int? month = null)
        {
            var defaultPeriod = BanglaHelper.GetDefaultBillingPeriod();
            var targetYear = year ?? defaultPeriod.Year;
            var targetMonth = month ?? defaultPeriod.Month;

            var setting = await _context.Settings.FirstOrDefaultAsync() ?? new Setting();
            var allRooms = await _context.Rooms
                .Include(r => r.Residents)
                .ToListAsync();

            allRooms = allRooms
                .OrderBy(r => BanglaHelper.ExtractRoomNumberSortValue(r.RoomNumber))
                .ThenBy(r => r.RoomNumber)
                .ToList();

            var targetMonthBills = await _context.MonthlyBills
                .Include(b => b.Room)
                .Where(b => b.BillingYear == targetYear && b.BillingMonthNumber == targetMonth)
                .ToListAsync();

            var recentBills = await _context.MonthlyBills
                .Include(b => b.Room)
                .OrderByDescending(b => b.BillingYear)
                .ThenByDescending(b => b.BillingMonthNumber)
                .ThenByDescending(b => b.Id)
                .Take(10)
                .ToListAsync();

            var totalRooms = allRooms.Count;
            var occupiedRooms = allRooms.Count(r => r.Status == "Occupied");
            var vacantRooms = allRooms.Count(r => r.Status == "Vacant");
            var inactiveRooms = allRooms.Count(r => r.Status == "Inactive");

            var totalRent = targetMonthBills.Sum(b => b.RoomRent);
            var totalElec = targetMonthBills.Sum(b => b.ElectricityBill);
            var totalBills = targetMonthBills.Sum(b => b.TotalBill);

            var paidAmount = targetMonthBills.Sum(b => b.EffectivePaidAmount);
            var dueAmount = targetMonthBills.Sum(b => b.EffectiveDueAmount);

            var paidCount = targetMonthBills.Count(b => b.PaymentStatus == "Paid");
            var dueCount = targetMonthBills.Count(b => b.PaymentStatus != "Paid");

            return new DashboardViewModel
            {
                HouseName = setting.HouseName,
                HouseAddress = setting.HouseAddress,
                CurrentUnitPrice = setting.CurrentUnitPrice,
                TotalRooms = totalRooms,
                OccupiedRooms = occupiedRooms,
                VacantRooms = vacantRooms,
                InactiveRooms = inactiveRooms,
                CurrentYear = targetYear,
                CurrentMonthNumber = targetMonth,
                CurrentMonthBangla = BanglaHelper.FormatBillingMonthBangla(targetYear, targetMonth),
                TotalRoomRent = totalRent,
                TotalElectricityBill = totalElec,
                TotalBillAmount = totalBills,
                PaidAmount = paidAmount,
                DueAmount = dueAmount,
                PaidRoomsCount = paidCount,
                DueRoomsCount = dueCount,
                RecentBills = recentBills,
                Rooms = allRooms
            };
        }
    }
}
