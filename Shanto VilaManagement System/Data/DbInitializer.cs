using Microsoft.EntityFrameworkCore;
using Shanto_VilaManagement_System.Helpers;
using Shanto_VilaManagement_System.Models;

namespace Shanto_VilaManagement_System.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(AppDbContext context)
        {
            await context.Database.EnsureCreatedAsync();

            // Ensure new columns exist for MonthlyBills table (Safe schema check for MySQL)
            try
            {
                var conn = context.Database.GetDbConnection();
                await conn.OpenAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'MonthlyBills' AND COLUMN_NAME IN ('PreviousDue', 'PaidAmount', 'DueAmount', 'OtherBill');
                ";
                var existingCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        existingCols.Add(reader.GetString(0));
                    }
                }

                if (!existingCols.Contains("PreviousDue"))
                {
                    using var alterCmd = conn.CreateCommand();
                    alterCmd.CommandText = "ALTER TABLE `MonthlyBills` ADD COLUMN `PreviousDue` decimal(12,2) NOT NULL DEFAULT 0.00;";
                    await alterCmd.ExecuteNonQueryAsync();
                }
                if (!existingCols.Contains("PaidAmount"))
                {
                    using var alterCmd = conn.CreateCommand();
                    alterCmd.CommandText = "ALTER TABLE `MonthlyBills` ADD COLUMN `PaidAmount` decimal(12,2) NOT NULL DEFAULT 0.00;";
                    await alterCmd.ExecuteNonQueryAsync();
                }
                if (!existingCols.Contains("DueAmount"))
                {
                    using var alterCmd = conn.CreateCommand();
                    alterCmd.CommandText = "ALTER TABLE `MonthlyBills` ADD COLUMN `DueAmount` decimal(12,2) NOT NULL DEFAULT 0.00;";
                    await alterCmd.ExecuteNonQueryAsync();
                }
                if (!existingCols.Contains("OtherBill"))
                {
                    using var alterCmd = conn.CreateCommand();
                    alterCmd.CommandText = "ALTER TABLE `MonthlyBills` ADD COLUMN `OtherBill` decimal(12,2) NOT NULL DEFAULT 0.00;";
                    await alterCmd.ExecuteNonQueryAsync();
                }

                // Check and add OtherBill1To31, OtherBill32To42 and DefaultOtherBill to Settings table
                using var setColCmd = conn.CreateCommand();
                setColCmd.CommandText = @"
                    SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Settings' AND COLUMN_NAME IN ('OtherBill1To31', 'OtherBill32To42', 'DefaultOtherBill');
                ";
                var existingSetCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var setReader = await setColCmd.ExecuteReaderAsync())
                {
                    while (await setReader.ReadAsync())
                    {
                        existingSetCols.Add(setReader.GetString(0));
                    }
                }
                if (!existingSetCols.Contains("DefaultOtherBill"))
                {
                    using var alterSetCmd = conn.CreateCommand();
                    alterSetCmd.CommandText = "ALTER TABLE `Settings` ADD COLUMN `DefaultOtherBill` decimal(12,2) NOT NULL DEFAULT 0.00;";
                    await alterSetCmd.ExecuteNonQueryAsync();
                }
                if (!existingSetCols.Contains("OtherBill1To31"))
                {
                    using var alterSetCmd = conn.CreateCommand();
                    alterSetCmd.CommandText = "ALTER TABLE `Settings` ADD COLUMN `OtherBill1To31` decimal(12,2) NOT NULL DEFAULT 0.00;";
                    await alterSetCmd.ExecuteNonQueryAsync();
                }
                if (!existingSetCols.Contains("OtherBill32To42"))
                {
                    using var alterSetCmd = conn.CreateCommand();
                    alterSetCmd.CommandText = "ALTER TABLE `Settings` ADD COLUMN `OtherBill32To42` decimal(12,2) NOT NULL DEFAULT 0.00;";
                    await alterSetCmd.ExecuteNonQueryAsync();
                }

                // Check and add OtherBill to Rooms table
                using var roomColCmd = conn.CreateCommand();
                roomColCmd.CommandText = "SELECT COLUMN_NAME FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'Rooms' AND COLUMN_NAME = 'OtherBill';";
                var roomCol = await roomColCmd.ExecuteScalarAsync();
                if (roomCol == null)
                {
                    using var alterRoomCmd = conn.CreateCommand();
                    alterRoomCmd.CommandText = "ALTER TABLE `Rooms` ADD COLUMN `OtherBill` decimal(12,2) NULL DEFAULT NULL;";
                    await alterRoomCmd.ExecuteNonQueryAsync();
                }

                using var updateCmd = conn.CreateCommand();
                updateCmd.CommandText = @"
                    UPDATE `MonthlyBills` SET `PaidAmount` = `TotalBill`, `DueAmount` = 0 WHERE `PaymentStatus` = 'Paid' AND `PaidAmount` = 0;
                    UPDATE `MonthlyBills` SET `PaidAmount` = 0, `DueAmount` = `TotalBill` WHERE `PaymentStatus` = 'Unpaid' AND `DueAmount` = 0;
                ";
                await updateCmd.ExecuteNonQueryAsync();

                // Check and add AdvanceAmount to Residents table
                using var resColCmd = conn.CreateCommand();
                resColCmd.CommandText = "SELECT COLUMN_NAME FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'Residents' AND COLUMN_NAME = 'AdvanceAmount';";
                var advCol = await resColCmd.ExecuteScalarAsync();
                if (advCol == null)
                {
                    using var alterAdvCmd = conn.CreateCommand();
                    alterAdvCmd.CommandText = "ALTER TABLE `Residents` ADD COLUMN `AdvanceAmount` decimal(12,2) NOT NULL DEFAULT 0.00;";
                    await alterAdvCmd.ExecuteNonQueryAsync();
                }

                // Check and ensure unique index on MonthlyBills(RoomId, BillingYear, BillingMonthNumber)
                using var idxCmd = conn.CreateCommand();
                idxCmd.CommandText = "SELECT COUNT(*) FROM information_schema.statistics WHERE table_schema = DATABASE() AND table_name = 'MonthlyBills' AND index_name = 'UQ_MonthlyBills_Room_Year_Month';";
                var idxCount = Convert.ToInt64(await idxCmd.ExecuteScalarAsync());
                if (idxCount == 0)
                {
                    using var alterIdxCmd = conn.CreateCommand();
                    alterIdxCmd.CommandText = "CREATE UNIQUE INDEX `UQ_MonthlyBills_Room_Year_Month` ON `MonthlyBills` (`RoomId`, `BillingYear`, `BillingMonthNumber`);";
                    await alterIdxCmd.ExecuteNonQueryAsync();
                }
            }
            catch
            {
                // Silently ignore if already migrated or table doesn't exist yet
            }

            // 1. Seed Settings if not exists or update address
            var setting = await context.Settings.FirstOrDefaultAsync();
            if (setting == null)
            {
                context.Settings.Add(new Setting
                {
                    HouseName = "শান্ত ভিলা",
                    HouseAddress = "টেকপাড়া, কুনিয়া পাছর উত্তর, গাজীপুর সদর, গাজীপুর।",
                    CurrentUnitPrice = 12.00m,
                    UpdatedAt = DateTime.Now
                });
                await context.SaveChangesAsync();
            }
            else if (setting.HouseAddress != "টেকপাড়া, কুনিয়া পাছর উত্তর, গাজীপুর সদর, গাজীপুর।")
            {
                setting.HouseAddress = "টেকপাড়া, কুনিয়া পাছর উত্তর, গাজীপুর সদর, গাজীপুর।";
                await context.SaveChangesAsync();
            }

            // 2. Seed Initial Rooms and Residents if empty
            if (!await context.Rooms.AnyAsync())
            {
                var room101 = new Room
                {
                    RoomNumber = "১০১",
                    RoomRent = 8000m,
                    MeterNumber = "MTR-101",
                    Status = "Occupied",
                    CreatedAt = DateTime.Now.AddMonths(-3)
                };

                var room102 = new Room
                {
                    RoomNumber = "১০২",
                    RoomRent = 7500m,
                    MeterNumber = "MTR-102",
                    Status = "Occupied",
                    CreatedAt = DateTime.Now.AddMonths(-3)
                };

                var room103 = new Room
                {
                    RoomNumber = "১০৩",
                    RoomRent = 9000m,
                    MeterNumber = "MTR-103",
                    Status = "Occupied",
                    CreatedAt = DateTime.Now.AddMonths(-2)
                };

                var room104 = new Room
                {
                    RoomNumber = "১০৪",
                    RoomRent = 8500m,
                    MeterNumber = "MTR-104",
                    Status = "Vacant",
                    CreatedAt = DateTime.Now.AddMonths(-1)
                };

                var room201 = new Room
                {
                    RoomNumber = "২০১",
                    RoomRent = 10000m,
                    MeterNumber = "MTR-201",
                    Status = "Vacant",
                    CreatedAt = DateTime.Now.AddMonths(-1)
                };

                context.Rooms.AddRange(room101, room102, room103, room104, room201);
                await context.SaveChangesAsync();

                // Residents for Room 101 (Per user requirement spec: Rahim, Sumaiya, Arif, Ayesha)
                var residentRahim = new Resident
                {
                    RoomId = room101.Id,
                    Name = "রহিম উদ্দিন",
                    Phone = "01711002233",
                    Profession = "ব্যাংক কর্মকর্তা",
                    Organization = "সোনালী ব্যাংক পিএলসি",
                    CurrentAddress = "রুম ১০১, শান্ত ভিলা, ঢাকা",
                    PermanentAddress = "গ্রাম: চাঁদপুর, থানা: মতলব, জেলা: চাঁদপুর",
                    Relationship = "প্রধান ভাড়াটিয়া",
                    IsPrimaryTenant = true,
                    DateOfBirthOrAge = "৩৫ বছর",
                    CreatedAt = DateTime.Now.AddMonths(-3)
                };

                var residentSumaiya = new Resident
                {
                    RoomId = room101.Id,
                    Name = "সুমাইয়া আক্তার",
                    Phone = "01811223344",
                    Profession = "গৃহিণী",
                    Organization = "—",
                    CurrentAddress = "রুম ১০১, শান্ত ভিলা, ঢাকা",
                    PermanentAddress = "গ্রাম: চাঁদপুর, থানা: মতলব, জেলা: চাঁদপুর",
                    Relationship = "স্ত্রী",
                    IsPrimaryTenant = false,
                    DateOfBirthOrAge = "২৮ বছর",
                    CreatedAt = DateTime.Now.AddMonths(-3)
                };

                var residentArif = new Resident
                {
                    RoomId = room101.Id,
                    Name = "আরিফ হোসেন",
                    Phone = "01911334455",
                    Profession = "ছাত্র",
                    Organization = "ঢাকা কলেজ",
                    CurrentAddress = "রুম ১০১, শান্ত ভিলা, ঢাকা",
                    PermanentAddress = "গ্রাম: চাঁদপুর, থানা: মতলব, জেলা: চাঁদপুর",
                    Relationship = "ছেলে",
                    IsPrimaryTenant = false,
                    DateOfBirthOrAge = "১০ বছর",
                    CreatedAt = DateTime.Now.AddMonths(-3)
                };

                var residentAyesha = new Resident
                {
                    RoomId = room101.Id,
                    Name = "আয়েশা খাতুন",
                    Phone = "—",
                    Profession = "শিশু",
                    Organization = "—",
                    CurrentAddress = "রুম ১০১, শান্ত ভিলা, ঢাকা",
                    PermanentAddress = "গ্রাম: চাঁদপুর, থানা: মতলব, জেলা: চাঁদপুর",
                    Relationship = "মেয়ে",
                    IsPrimaryTenant = false,
                    DateOfBirthOrAge = "৫ বছর",
                    CreatedAt = DateTime.Now.AddMonths(-3)
                };

                // Residents for Room 102
                var residentKarim = new Resident
                {
                    RoomId = room102.Id,
                    Name = "করিম মিয়া",
                    Phone = "01722445566",
                    Profession = "সফটওয়্যার ইঞ্জিনিয়ার",
                    Organization = "টেক ভিশন লিমিটেড",
                    CurrentAddress = "রুম ১০২, শান্ত ভিলা, ঢাকা",
                    PermanentAddress = "কুমিল্লা সদর, কুমিল্লা",
                    Relationship = "প্রধান ভাড়াটিয়া",
                    IsPrimaryTenant = true,
                    DateOfBirthOrAge = "৩২ বছর",
                    CreatedAt = DateTime.Now.AddMonths(-3)
                };

                // Residents for Room 103
                var residentHasan = new Resident
                {
                    RoomId = room103.Id,
                    Name = "হাসান মাহমুদ",
                    Phone = "01633556677",
                    Profession = "ব্যবসায়ী",
                    Organization = "মেসার্স হাসান ট্রেডার্স",
                    CurrentAddress = "রুম ১০৩, শান্ত ভিলা, ঢাকা",
                    PermanentAddress = "বগুড়া সদর, বগুড়া",
                    Relationship = "প্রধান ভাড়াটিয়া",
                    IsPrimaryTenant = true,
                    DateOfBirthOrAge = "৪০ বছর",
                    CreatedAt = DateTime.Now.AddMonths(-2)
                };

                context.Residents.AddRange(residentRahim, residentSumaiya, residentArif, residentAyesha, residentKarim, residentHasan);

                // Emergency Contacts
                context.EmergencyContacts.AddRange(
                    new EmergencyContact
                    {
                        RoomId = room101.Id,
                        Name = "মোঃ জহিরুল ইসলাম",
                        Relationship = "চাচাতো ভাই",
                        Phone = "01755123456",
                        Address = "মিরপুর ১০, ঢাকা"
                    },
                    new EmergencyContact
                    {
                        RoomId = room102.Id,
                        Name = "আব্দুর রব",
                        Relationship = "পিতা",
                        Phone = "01855987654",
                        Address = "কুমিল্লা সদর, কুমিল্লা"
                    }
                );

                await context.SaveChangesAsync();

                // 3. Seed Sample Historical Bills
                // October 2026 Bill for Room 101 (from user requirement example):
                // Previous: 1250, Current: 1380, Used: 130, UnitPrice: 12, Electricity: 1560, Rent: 8000, Total: 9560, Paid: 05/11/2026
                var bill101Oct = new MonthlyBill
                {
                    RoomId = room101.Id,
                    BillingMonth = BanglaHelper.FormatBillingMonthBangla(2026, 10),
                    BillingYear = 2026,
                    BillingMonthNumber = 10,
                    TenantName = residentRahim.Name,
                    RoomRent = 8000m,
                    PreviousReading = 1250m,
                    CurrentReading = 1380m,
                    UsedUnit = 130m,
                    UnitPrice = 12.00m,
                    ElectricityBill = 1560m,
                    TotalBill = 9560m,
                    PreviousDue = 0m,
                    PaidAmount = 9560m,
                    DueAmount = 0m,
                    PaymentStatus = "Paid",
                    PaidDate = new DateTime(2026, 11, 5),
                    CreatedAt = new DateTime(2026, 10, 31)
                };

                // Room 102 October 2026: Previous 850, Current 920, Used 70, Elec 840, Rent 7500, Total 8340, Unpaid
                var bill102Oct = new MonthlyBill
                {
                    RoomId = room102.Id,
                    BillingMonth = BanglaHelper.FormatBillingMonthBangla(2026, 10),
                    BillingYear = 2026,
                    BillingMonthNumber = 10,
                    TenantName = residentKarim.Name,
                    RoomRent = 7500m,
                    PreviousReading = 850m,
                    CurrentReading = 920m,
                    UsedUnit = 70m,
                    UnitPrice = 12.00m,
                    ElectricityBill = 840m,
                    TotalBill = 8340m,
                    PreviousDue = 0m,
                    PaidAmount = 0m,
                    DueAmount = 8340m,
                    PaymentStatus = "Unpaid",
                    PaidDate = null,
                    CreatedAt = new DateTime(2026, 10, 31)
                };

                // Room 103 October 2026: Previous 2100, Current 2225, Used 125, Elec 1500, Rent 9000, Total 10500, Paid: 03/11/2026
                var bill103Oct = new MonthlyBill
                {
                    RoomId = room103.Id,
                    BillingMonth = BanglaHelper.FormatBillingMonthBangla(2026, 10),
                    BillingYear = 2026,
                    BillingMonthNumber = 10,
                    TenantName = residentHasan.Name,
                    RoomRent = 9000m,
                    PreviousReading = 2100m,
                    CurrentReading = 2225m,
                    UsedUnit = 125m,
                    UnitPrice = 12.00m,
                    ElectricityBill = 1500m,
                    TotalBill = 10500m,
                    PreviousDue = 0m,
                    PaidAmount = 10500m,
                    DueAmount = 0m,
                    PaymentStatus = "Paid",
                    PaidDate = new DateTime(2026, 11, 3),
                    CreatedAt = new DateTime(2026, 10, 31)
                };

                context.MonthlyBills.AddRange(bill101Oct, bill102Oct, bill103Oct);
                await context.SaveChangesAsync();
            }
        }
    }
}
