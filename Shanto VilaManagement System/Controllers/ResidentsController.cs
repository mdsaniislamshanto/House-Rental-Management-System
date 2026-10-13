using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Shanto_VilaManagement_System.Data;
using Shanto_VilaManagement_System.Helpers;
using Shanto_VilaManagement_System.Models;
using Shanto_VilaManagement_System.ViewModels;
using Shanto_VilaManagement_System.Services;

namespace Shanto_VilaManagement_System.Controllers
{
    public class ResidentsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ICloudStorageService _cloudStorage;

        public ResidentsController(AppDbContext context, IWebHostEnvironment env, ICloudStorageService cloudStorage)
        {
            _context = context;
            _env = env;
            _cloudStorage = cloudStorage;
        }

        // GET: Residents
        public async Task<IActionResult> Index(string? tab, int? roomId)
        {
            var activeTab = string.Equals(tab, "archived", StringComparison.OrdinalIgnoreCase) ? "archived" : "active";

            var allRooms = await _context.Rooms
                .Include(r => r.Residents)
                .ToListAsync();

            allRooms = allRooms
                .OrderBy(r => BanglaHelper.ExtractRoomNumberSortValue(r.RoomNumber))
                .ThenBy(r => r.RoomNumber)
                .ToList();

            var queryRooms = allRooms.AsEnumerable();
            if (roomId.HasValue)
            {
                queryRooms = queryRooms.Where(r => r.Id == roomId.Value);
            }

            var roomGroups = new List<RoomResidentsGroupViewModel>();
            foreach (var room in queryRooms)
            {
                var activeRes = room.Residents
                    .Where(r => r.IsActive)
                    .OrderByDescending(r => r.IsPrimaryTenant)
                    .ThenBy(r => r.CreatedAt)
                    .ToList();

                if (activeRes.Any())
                {
                    roomGroups.Add(new RoomResidentsGroupViewModel
                    {
                        Room = room,
                        ActiveResidents = activeRes,
                        PrimaryTenant = activeRes.FirstOrDefault(r => r.IsPrimaryTenant) ?? activeRes.FirstOrDefault()
                    });
                }
            }

            var archivedQuery = _context.Residents
                .Include(r => r.Room)
                .Where(r => !r.IsActive)
                .AsQueryable();

            if (roomId.HasValue)
            {
                archivedQuery = archivedQuery.Where(r => r.RoomId == roomId.Value);
            }

            var archivedResidents = await archivedQuery
                .OrderByDescending(r => r.LeftAt ?? r.CreatedAt)
                .ToListAsync();

            var archivedRoomGroups = new List<RoomArchivedGroupViewModel>();
            var groupedByRoom = archivedResidents.GroupBy(r => r.RoomId);

            foreach (var group in groupedByRoom)
            {
                var room = allRooms.FirstOrDefault(r => r.Id == group.Key) ?? group.First().Room ?? new Room { RoomNumber = group.Key.ToString() };
                var list = group.OrderByDescending(r => r.IsPrimaryTenant).ThenByDescending(r => r.LeftAt).ToList();
                var primary = list.FirstOrDefault(r => r.IsPrimaryTenant) ?? list.FirstOrDefault();

                archivedRoomGroups.Add(new RoomArchivedGroupViewModel
                {
                    Room = room,
                    PrimaryTenant = primary,
                    ArchivedResidents = list
                });
            }

            archivedRoomGroups = archivedRoomGroups
                .OrderBy(g => BanglaHelper.ExtractRoomNumberSortValue(g.Room?.RoomNumber))
                .ThenBy(g => g.Room?.RoomNumber)
                .ToList();

            var totalActiveCount = await _context.Residents.CountAsync(r => r.IsActive);
            var totalArchivedCount = await _context.Residents.CountAsync(r => !r.IsActive);

            var viewModel = new ResidentsIndexViewModel
            {
                ActiveTab = activeTab,
                RoomGroups = roomGroups,
                ArchivedRoomGroups = archivedRoomGroups,
                ArchivedResidents = archivedResidents,
                TotalActiveResidentsCount = totalActiveCount,
                TotalArchivedResidentsCount = totalArchivedCount,
                SelectedRoomId = roomId,
                AllRooms = allRooms
            };

            return View(viewModel);
        }

        // GET: Residents/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var resident = await _context.Residents
                .Include(r => r.Room)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (resident == null) return NotFound();

            return View(resident);
        }

        // GET: Residents/Create
        public async Task<IActionResult> Create(int? roomId, bool isAddMember = false)
        {
            var rooms = await _context.Rooms
                .Include(r => r.Residents)
                .ToListAsync();

            rooms = rooms
                .OrderBy(r => BanglaHelper.ExtractRoomNumberSortValue(r.RoomNumber))
                .ThenBy(r => r.RoomNumber)
                .ToList();

            var selectListItems = new List<SelectListItem>();
            var vacantRooms = new List<Room>();

            foreach (var r in rooms)
            {
                var hasActiveResidents = r.Residents.Any(res => res.IsActive);
                if (hasActiveResidents)
                {
                    var primaryName = r.Residents.FirstOrDefault(res => res.IsActive && res.IsPrimaryTenant)?.Name 
                        ?? r.Residents.FirstOrDefault(res => res.IsActive)?.Name;

                    selectListItems.Add(new SelectListItem
                    {
                        Value = r.Id.ToString(),
                        Text = $"রুম {BanglaHelper.ToBanglaDigits(r.RoomNumber)} (ইতোমধ্যে ভাড়া দেওয়া: {primaryName})",
                        Disabled = !isAddMember
                    });
                }
                else
                {
                    vacantRooms.Add(r);
                    selectListItems.Add(new SelectListItem
                    {
                        Value = r.Id.ToString(),
                        Text = $"রুম {BanglaHelper.ToBanglaDigits(r.RoomNumber)} (খালি - ভাড়া: {BanglaHelper.ToBanglaCurrency(r.RoomRent)})"
                    });
                }
            }

            ViewBag.Rooms = rooms;
            ViewBag.AllRoomsOccupied = !vacantRooms.Any();
            ViewBag.IsAddMember = isAddMember;

            int effectiveRoomId = 0;
            if (isAddMember && roomId.HasValue)
            {
                effectiveRoomId = roomId.Value;
                var targetRoom = rooms.FirstOrDefault(r => r.Id == effectiveRoomId);
                var existingPrimary = targetRoom?.Residents.FirstOrDefault(res => res.IsActive && res.IsPrimaryTenant) 
                    ?? targetRoom?.Residents.FirstOrDefault(res => res.IsActive);
                ViewBag.ExistingPrimaryTenant = existingPrimary;
                ViewBag.TargetRoom = targetRoom;
            }
            else
            {
                // If roomId was passed for new tenant, verify it is vacant
                if (roomId.HasValue && vacantRooms.Any(vr => vr.Id == roomId.Value))
                {
                    effectiveRoomId = roomId.Value;
                }
                else
                {
                    effectiveRoomId = vacantRooms.FirstOrDefault()?.Id ?? 0;
                }
            }

            ViewBag.RoomId = new SelectList(selectListItems, "Value", "Text", effectiveRoomId);

            var model = new MultipleResidentsCreateViewModel
            {
                RoomId = effectiveRoomId,
                IsAddMember = isAddMember,
                AdultCount = 1,
                ChildCount = 0,
                Residents = new List<ResidentItemInputModel>
                {
                    new ResidentItemInputModel
                    {
                        Relationship = isAddMember ? "পরিবারের সদস্য" : "প্রধান ভাড়াটিয়া",
                        IsPrimaryTenant = !isAddMember,
                        IsChild = false
                    }
                }
            };
            return View(model);
        }

        // POST: Residents/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MultipleResidentsCreateViewModel model)
        {
            if (model.RoomId <= 0)
            {
                ModelState.AddModelError("RoomId", "দয়া করে একটি রুম নির্বাচন করুন।");
            }
            else
            {
                var hasActiveResidents = await _context.Residents
                    .AnyAsync(r => r.RoomId == model.RoomId && r.IsActive);

                if (hasActiveResidents && !model.IsAddMember)
                {
                    ModelState.AddModelError("RoomId", "এই রুমে ইতোমধ্যে বাসিন্দা / ভাড়াটিয়া রয়েছেন। একই রুমে নতুন করে অন্য কোনো ভাড়াটিয়া যুক্ত করা যাবে না।");
                }
            }

            if (!model.IsAddMember && model.AdultCount < 1)
            {
                ModelState.AddModelError("AdultCount", "নতুন রুম ভাড়ার ক্ষেত্রে কমপক্ষে ১ জন প্রাপ্তবয়স্ক ভাড়াটিয়া থাকা আবশ্যক।");
            }
            else if (model.IsAddMember && (model.AdultCount + model.ChildCount < 1))
            {
                ModelState.AddModelError("AdultCount", "কমপক্ষে একজন সদস্য (প্রাপ্তবয়স্ক অথবা শিশু) নির্বাচন করুন।");
            }

            var validResidents = model.Residents?
                .Where(r => !string.IsNullOrWhiteSpace(r.Name))
                .ToList() ?? new List<ResidentItemInputModel>();

            if (!validResidents.Any())
            {
                ModelState.AddModelError("Residents[0].Name", "কমপক্ষে একজন বাসিন্দার নাম প্রদান করতে হবে।");
            }

            if (ModelState.IsValid)
            {
                var room = await _context.Rooms.FindAsync(model.RoomId);
                if (room == null) return NotFound();

                if (!model.IsAddMember)
                {
                    // Determine primary tenant for new tenancy
                    var primaryInput = validResidents.FirstOrDefault(r => r.IsPrimaryTenant) ?? validResidents.FirstOrDefault();
                    if (primaryInput != null)
                    {
                        primaryInput.IsPrimaryTenant = true;
                    }
                }
                else
                {
                    // Adding members to existing room: never overwrite primary tenant
                    foreach (var v in validResidents)
                    {
                        v.IsPrimaryTenant = false;
                    }
                }

                var primaryPhone = validResidents.FirstOrDefault(r => r.IsPrimaryTenant)?.Phone?.Trim() ?? string.Empty;
                var primaryCurrentAddr = validResidents.FirstOrDefault(r => r.IsPrimaryTenant)?.CurrentAddress?.Trim();
                var primaryPermanentAddr = validResidents.FirstOrDefault(r => r.IsPrimaryTenant)?.PermanentAddress?.Trim();

                if (model.IsAddMember)
                {
                    var existingPrimary = await _context.Residents
                        .FirstOrDefaultAsync(r => r.RoomId == model.RoomId && r.IsActive && r.IsPrimaryTenant)
                        ?? await _context.Residents.FirstOrDefaultAsync(r => r.RoomId == model.RoomId && r.IsActive);

                    if (existingPrimary != null)
                    {
                        if (string.IsNullOrWhiteSpace(primaryPhone) || primaryPhone == "—") primaryPhone = existingPrimary.Phone;
                        if (string.IsNullOrWhiteSpace(primaryCurrentAddr)) primaryCurrentAddr = existingPrimary.CurrentAddress;
                        if (string.IsNullOrWhiteSpace(primaryPermanentAddr)) primaryPermanentAddr = existingPrimary.PermanentAddress;
                    }
                }

                // If any resident is marked as primary, reset existing primary tenants in this room (only for new room tenancy)
                if (!model.IsAddMember && validResidents.Any(r => r.IsPrimaryTenant))
                {
                    var existingPrimaries = await _context.Residents
                        .Where(r => r.RoomId == model.RoomId && r.IsPrimaryTenant)
                        .ToListAsync();

                    foreach (var p in existingPrimaries)
                    {
                        p.IsPrimaryTenant = false;
                    }
                }

                var savedResidents = new List<Resident>();
                var residentCount = model.Residents?.Count ?? 0;

                for (int i = 0; i < residentCount; i++)
                {
                    var item = model.Residents![i];
                    if (string.IsNullOrWhiteSpace(item.Name)) continue;

                    var advance = model.IsAddMember 
                        ? 0m 
                        : (item.AdvanceAmount > 0 ? item.AdvanceAmount : (item.IsPrimaryTenant || i == 0 ? model.AdvanceAmount : 0m));

                    var resident = new Resident
                    {
                        RoomId = model.RoomId,
                        Name = item.Name.Trim(),
                        Relationship = string.IsNullOrWhiteSpace(item.Relationship) 
                            ? (item.IsChild ? "অন্যান্য" : (model.IsAddMember ? "পরিবারের সদস্য" : "প্রধান ভাড়াটিয়া")) 
                            : item.Relationship.Trim(),
                        IsPrimaryTenant = model.IsAddMember ? false : item.IsPrimaryTenant,
                        AdvanceAmount = advance,
                        Profession = item.Profession?.Trim(),
                        Organization = item.Organization?.Trim(),
                        DateOfBirthOrAge = item.DateOfBirthOrAge?.Trim(),
                        CurrentAddress = !string.IsNullOrWhiteSpace(item.CurrentAddress) ? item.CurrentAddress.Trim() : primaryCurrentAddr,
                        PermanentAddress = !string.IsNullOrWhiteSpace(item.PermanentAddress) ? item.PermanentAddress.Trim() : primaryPermanentAddr,
                        CreatedAt = DateTime.Now
                    };

                    // Handle phone number
                    if (!string.IsNullOrWhiteSpace(item.Phone))
                    {
                        resident.Phone = item.Phone.Trim();
                    }
                    else if (!string.IsNullOrWhiteSpace(primaryPhone))
                    {
                        resident.Phone = primaryPhone;
                    }
                    else
                    {
                        resident.Phone = "—";
                    }

                    // Handle file uploads (photo, NID front, NID back)
                    var photoFile = item.PhotoFile 
                        ?? Request.Form.Files[$"Residents[{i}].PhotoFile"] 
                        ?? Request.Form.Files[$"photoFile_{i}"];
                    if (photoFile != null && photoFile.Length > 0)
                    {
                        resident.Photo = await _cloudStorage.SaveFileAsync(photoFile, "residents/photos");
                    }

                    var nidFrontFile = item.NidFrontFile 
                        ?? Request.Form.Files[$"Residents[{i}].NidFrontFile"] 
                        ?? Request.Form.Files[$"nidFrontFile_{i}"];
                    if (nidFrontFile != null && nidFrontFile.Length > 0)
                    {
                        resident.NIDFront = await _cloudStorage.SaveFileAsync(nidFrontFile, "residents/nid");
                    }

                    var nidBackFile = item.NidBackFile 
                        ?? Request.Form.Files[$"Residents[{i}].NidBackFile"] 
                        ?? Request.Form.Files[$"nidBackFile_{i}"];
                    if (nidBackFile != null && nidBackFile.Length > 0)
                    {
                        resident.NIDBack = await _cloudStorage.SaveFileAsync(nidBackFile, "residents/nid");
                    }

                    _context.Residents.Add(resident);
                    savedResidents.Add(resident);
                }

                // Automatically update room status to Occupied if it is currently Vacant
                if (room.Status == "Vacant")
                {
                    room.Status = "Occupied";
                }

                await _context.SaveChangesAsync();

                if (model.IsAddMember)
                {
                    TempData["SuccessMessage"] = $"রুমের মোট {BanglaHelper.ToBanglaDigits(savedResidents.Count)} জন নতুন সদস্য সফলভাবে যুক্ত করা হয়েছে।";
                }
                else
                {
                    TempData["SuccessMessage"] = $"রুমের মোট {BanglaHelper.ToBanglaDigits(savedResidents.Count)} জন বাসিন্দার তথ্য সফলভাবে যুক্ত করা হয়েছে।";
                }
                return RedirectToAction("Details", "Rooms", new { id = model.RoomId });
            }

            var allRooms = await _context.Rooms
                .Include(r => r.Residents)
                .ToListAsync();
            allRooms = allRooms
                .OrderBy(r => BanglaHelper.ExtractRoomNumberSortValue(r.RoomNumber))
                .ThenBy(r => r.RoomNumber)
                .ToList();

            var fallbackItems = new List<SelectListItem>();
            foreach (var r in allRooms)
            {
                var hasActive = r.Residents.Any(res => res.IsActive);
                if (hasActive)
                {
                    var primaryName = r.Residents.FirstOrDefault(res => res.IsActive && res.IsPrimaryTenant)?.Name 
                        ?? r.Residents.FirstOrDefault(res => res.IsActive)?.Name;

                    fallbackItems.Add(new SelectListItem
                    {
                        Value = r.Id.ToString(),
                        Text = $"রুম {BanglaHelper.ToBanglaDigits(r.RoomNumber)} (ইতোমধ্যে ভাড়া দেওয়া: {primaryName})",
                        Disabled = !model.IsAddMember
                    });
                }
                else
                {
                    fallbackItems.Add(new SelectListItem
                    {
                        Value = r.Id.ToString(),
                        Text = $"রুম {BanglaHelper.ToBanglaDigits(r.RoomNumber)} (খালি - ভাড়া: {BanglaHelper.ToBanglaCurrency(r.RoomRent)})"
                    });
                }
            }

            ViewBag.Rooms = allRooms;
            ViewBag.IsAddMember = model.IsAddMember;
            ViewBag.RoomId = new SelectList(fallbackItems, "Value", "Text", model.RoomId);

            return View(model);
        }

        // GET: Residents/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var resident = await _context.Residents.FindAsync(id);
            if (resident == null) return NotFound();

            var editRooms = await _context.Rooms.ToListAsync();
            editRooms = editRooms
                .OrderBy(r => BanglaHelper.ExtractRoomNumberSortValue(r.RoomNumber))
                .ThenBy(r => r.RoomNumber)
                .ToList();

            ViewBag.RoomId = new SelectList(editRooms, "Id", "RoomNumber", resident.RoomId);
            return View(resident);
        }

        // POST: Residents/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,RoomId,Name,Phone,CurrentAddress,PermanentAddress,Profession,Organization,DateOfBirthOrAge,Relationship,IsPrimaryTenant,AdvanceAmount,Photo,NIDFront,NIDBack,CreatedAt")] Resident resident,
            IFormFile? photoFile,
            IFormFile? nidFrontFile,
            IFormFile? nidBackFile)
        {
            if (id != resident.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // If new photo uploaded, remove old and save new
                    if (photoFile != null)
                    {
                        await _cloudStorage.DeleteFileAsync(resident.Photo);
                        resident.Photo = await _cloudStorage.SaveFileAsync(photoFile, "residents/photos");
                    }
                    if (nidFrontFile != null)
                    {
                        await _cloudStorage.DeleteFileAsync(resident.NIDFront);
                        resident.NIDFront = await _cloudStorage.SaveFileAsync(nidFrontFile, "residents/nid");
                    }
                    if (nidBackFile != null)
                    {
                        await _cloudStorage.DeleteFileAsync(resident.NIDBack);
                        resident.NIDBack = await _cloudStorage.SaveFileAsync(nidBackFile, "residents/nid");
                    }

                    // If marked as primary tenant, reset other primary tenants in same room
                    if (resident.IsPrimaryTenant)
                    {
                        var existingPrimaries = await _context.Residents
                            .Where(r => r.RoomId == resident.RoomId && r.Id != resident.Id && r.IsPrimaryTenant)
                            .ToListAsync();

                        foreach (var p in existingPrimaries)
                        {
                            p.IsPrimaryTenant = false;
                        }
                    }

                    _context.Update(resident);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"{resident.Name}-এর তথ্য সফলভাবে আপডেট করা হয়েছে।";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Residents.AnyAsync(e => e.Id == resident.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Details), new { id = resident.Id });
            }

            var failRooms = await _context.Rooms.ToListAsync();
            failRooms = failRooms
                .OrderBy(r => BanglaHelper.ExtractRoomNumberSortValue(r.RoomNumber))
                .ThenBy(r => r.RoomNumber)
                .ToList();
            ViewBag.RoomId = new SelectList(failRooms, "Id", "RoomNumber", resident.RoomId);
            return View(resident);
        }

        // POST: Residents/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            return await SoftDelete(id);
        }

        // POST: Residents/SoftDelete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoftDelete(int id)
        {
            var resident = await _context.Residents.FindAsync(id);
            if (resident == null) return NotFound();

            var roomId = resident.RoomId;
            resident.IsActive = false;
            resident.LeftAt = DateTime.Now;

            // If primary tenant, assign another active resident in this room as primary tenant
            if (resident.IsPrimaryTenant)
            {
                resident.IsPrimaryTenant = false;
                var nextPrimary = await _context.Residents
                    .Where(r => r.RoomId == roomId && r.Id != id && r.IsActive)
                    .FirstOrDefaultAsync();

                if (nextPrimary != null)
                {
                    nextPrimary.IsPrimaryTenant = true;
                }
            }

            // Check if room has any remaining active residents
            var hasActive = await _context.Residents.AnyAsync(r => r.RoomId == roomId && r.Id != id && r.IsActive);
            if (!hasActive)
            {
                var room = await _context.Rooms.FindAsync(roomId);
                if (room != null && room.Status == "Occupied")
                {
                    room.Status = "Vacant";
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"{resident.Name}-কে সফলভাবে প্রাক্তন / প্রস্থানকারী হিসেবে তালিকাভুক্ত করা হয়েছে (সফ্ট ডিলিট)।";
            return RedirectToAction(nameof(Index), new { roomId });
        }

        // POST: Residents/VacateRoom
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VacateRoom(int roomId)
        {
            var activeResidents = await _context.Residents
                .Where(r => r.RoomId == roomId && r.IsActive)
                .ToListAsync();

            if (!activeResidents.Any())
            {
                TempData["ErrorMessage"] = "এই রুমে বর্তমানে কোনো সক্রিয় বাসিন্দা নেই।";
                return RedirectToAction(nameof(Index));
            }

            foreach (var res in activeResidents)
            {
                res.IsActive = false;
                res.LeftAt = DateTime.Now;
                res.IsPrimaryTenant = false;
            }

            var room = await _context.Rooms.FindAsync(roomId);
            if (room != null)
            {
                room.Status = "Vacant";
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"রুমের মোট {BanglaHelper.ToBanglaDigits(activeResidents.Count)} জন বাসিন্দার প্রস্থান সম্পন্ন হয়েছে এবং রুমটি খালি হিসেবে চিহ্নিত হয়েছে।";
            return RedirectToAction(nameof(Index));
        }

        // POST: Residents/PermanentDelete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PermanentDelete(int id)
        {
            var resident = await _context.Residents.FindAsync(id);
            if (resident == null) return NotFound();

            var name = resident.Name;

            // Delete physical/cloud files
            await _cloudStorage.DeleteFileAsync(resident.Photo);
            await _cloudStorage.DeleteFileAsync(resident.NIDFront);
            await _cloudStorage.DeleteFileAsync(resident.NIDBack);

            _context.Residents.Remove(resident);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{name}-এর তথ্য ডাটাবেজ থেকে স্থায়ীভাবে মুছে ফেলা হয়েছে।";
            return RedirectToAction(nameof(Index), new { tab = "archived" });
        }

        // POST: Residents/Restore/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var resident = await _context.Residents.FindAsync(id);
            if (resident == null) return NotFound();

            resident.IsActive = true;
            resident.LeftAt = null;

            // Check if room already has a primary tenant
            var hasPrimary = await _context.Residents.AnyAsync(r => r.RoomId == resident.RoomId && r.Id != id && r.IsActive && r.IsPrimaryTenant);
            if (!hasPrimary)
            {
                resident.IsPrimaryTenant = true;
            }

            var room = await _context.Rooms.FindAsync(resident.RoomId);
            if (room != null && room.Status == "Vacant")
            {
                room.Status = "Occupied";
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"{resident.Name}-কে পুনরায় সক্রিয় বাসিন্দা হিসেবে সফলভাবে যুক্ত করা হয়েছে।";
            return RedirectToAction(nameof(Index), new { roomId = resident.RoomId });
        }

        // POST: Residents/PermanentDeleteRoom
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PermanentDeleteRoom(int roomId)
        {
            var archivedResidents = await _context.Residents
                .Where(r => r.RoomId == roomId && !r.IsActive)
                .ToListAsync();

            if (!archivedResidents.Any())
            {
                TempData["ErrorMessage"] = "এই রুমের কোনো প্রাক্তন বাসিন্দার রেকর্ড পাওয়া যায়নি।";
                return RedirectToAction(nameof(Index), new { tab = "archived" });
            }

            var count = archivedResidents.Count;
            foreach (var res in archivedResidents)
            {
                await _cloudStorage.DeleteFileAsync(res.Photo);
                await _cloudStorage.DeleteFileAsync(res.NIDFront);
                await _cloudStorage.DeleteFileAsync(res.NIDBack);
                _context.Residents.Remove(res);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"রুমের মোট {BanglaHelper.ToBanglaDigits(count)} জন প্রাক্তন বাসিন্দার সকল তথ্য ডাটাবেজ থেকে স্থায়ীভাবে মুছে ফেলা হয়েছে।";
            return RedirectToAction(nameof(Index), new { tab = "archived" });
        }

        // POST: Residents/RestoreRoom
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreRoom(int roomId)
        {
            var archivedResidents = await _context.Residents
                .Where(r => r.RoomId == roomId && !r.IsActive)
                .ToListAsync();

            if (!archivedResidents.Any())
            {
                TempData["ErrorMessage"] = "পুনরুদ্ধার করার মতো কোনো প্রাক্তন বাসিন্দা পাওয়া যায়নি।";
                return RedirectToAction(nameof(Index), new { tab = "archived" });
            }

            foreach (var res in archivedResidents)
            {
                res.IsActive = true;
                res.LeftAt = null;
            }

            var primary = archivedResidents.FirstOrDefault(r => r.IsPrimaryTenant) ?? archivedResidents.FirstOrDefault();
            if (primary != null)
            {
                primary.IsPrimaryTenant = true;
            }

            var room = await _context.Rooms.FindAsync(roomId);
            if (room != null && room.Status == "Vacant")
            {
                room.Status = "Occupied";
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"রুমের সকল বাসিন্দাকে পুনরায় সক্রিয় বাসিন্দা হিসেবে যুক্ত করা হয়েছে।";
            return RedirectToAction(nameof(Index), new { roomId });
        }
    }
}
