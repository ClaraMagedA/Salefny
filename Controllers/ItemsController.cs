using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalefnyMVC.Data;
using SalefnyMVC.Models.Entities;
using SalefnyMVC.Models.Enums;
using SalefnyMVC.Services;
using SalefnyMVC.ViewModels;
using System.Security.Claims;

namespace Salefny.Controllers
{
    public class ItemsController : Controller
    {
        private readonly SalefnyDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly UserManager<ApplicationUser> _userManager;

        public ItemsController(
            SalefnyDbContext context,
            IWebHostEnvironment environment,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _environment = environment;
            _userManager = userManager;
        }

        private async Task<bool> CanCreateListingsAsync()
        {
            if (User.Identity?.IsAuthenticated != true) return false;

            var user = await _userManager.GetUserAsync(User);
            return user != null && await _userManager.IsInRoleAsync(user, AppRoles.Owner);
        }

        public async Task<IActionResult> Index(ItemSearchViewModel? model)
        {
            model ??= new ItemSearchViewModel();
            model.Keyword ??= string.Empty;
            model.City ??= string.Empty;
            var sortKey = string.IsNullOrWhiteSpace(model.Sort)
                ? "newest"
                : model.Sort.Trim().ToLowerInvariant();
            // Keep the canonical values used by the sort dropdown so the selected
            // option survives a postback.
            model.Sort = sortKey switch
            {
                "priceasc" => "priceAsc",
                "pricedesc" => "priceDesc",
                "rating" => "rating",
                _ => "newest"
            };
            model.Page = Math.Max(1, model.Page);
            model.PageSize = 12;

            // Only approved, published listings are searchable in the public souq.
            var approvedItems = _context.Items
                .AsNoTracking()
                .Where(i => i.IsActive && i.ApprovalStatus == ListingApprovalStatus.Approved);

            // Cities come from the catalogue itself, never from a hardcoded list.
            model.Cities = await approvedItems
                .Where(i => !string.IsNullOrWhiteSpace(i.City))
                .Select(i => i.City)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            model.Categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.Id)
                .ToListAsync();

            IQueryable<Item> query = approvedItems
                .Include(i => i.Category)
                .Include(i => i.ItemImages)
                .Include(i => i.Owner);

            var keyword = model.Keyword.Trim();
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(i => i.Title.Contains(keyword)
                    || (i.Description != null && i.Description.Contains(keyword)));
            }

            if (!string.IsNullOrWhiteSpace(model.City))
                query = query.Where(i => i.City == model.City);

            if (model.CategoryId.HasValue)
                query = query.Where(i => i.CategoryId == model.CategoryId.Value);

            if (model.MinPrice.HasValue)
                query = query.Where(i => i.PricePerDay >= model.MinPrice.Value);

            if (model.MaxPrice.HasValue)
                query = query.Where(i => i.PricePerDay <= model.MaxPrice.Value);

            switch (model.Sort)
            {
                case "priceAsc":
                    query = query.OrderBy(i => i.PricePerDay).ThenByDescending(i => i.CreatedAt);
                    break;
                case "priceDesc":
                    query = query.OrderByDescending(i => i.PricePerDay).ThenByDescending(i => i.CreatedAt);
                    break;
                case "rating":
                    query = query.OrderByDescending(i => i.Owner.TrustRating).ThenByDescending(i => i.CreatedAt);
                    break;
                default:
                    query = query.OrderByDescending(i => i.CreatedAt).ThenByDescending(i => i.Id);
                    break;
            }

            model.TotalCount = await query.CountAsync();
            model.TotalPages = model.TotalCount == 0
                ? 0
                : (int)Math.Ceiling(model.TotalCount / (double)model.PageSize);
            if (model.TotalPages > 0 && model.Page > model.TotalPages)
                model.Page = model.TotalPages;

            model.Items = await query
                .Skip((model.Page - 1) * model.PageSize)
                .Take(model.PageSize)
                .ToListAsync();

            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var item = await _context.Items
                .Include(i => i.Category)
                .Include(i => i.ItemImages)
                .Include(i => i.Owner)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (item == null) return NotFound();

            // Pending or rejected listings are visible only to their owner and admins.
            var canReview = false;
            if (User.Identity?.IsAuthenticated == true)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null)
                {
                    canReview = item.OwnerId == currentUser.Id
                        || await _userManager.IsInRoleAsync(currentUser, AppRoles.Admin)
                        || await _userManager.IsInRoleAsync(currentUser, AppRoles.SuperAdmin);
                }
            }

            if (!canReview && item.ApprovalStatus != ListingApprovalStatus.Approved)
                return NotFound();

            var today = DateTime.Today;
            var windows = new List<(DateTime Start, DateTime End, string Source)>();

            if (item != null)
            {
                var bookings = await _context.Bookings
                    .Where(b => b.ItemId == id && b.EndDate >= today
                                && b.Status != BookingStatus.Cancelled
                                && b.Status != BookingStatus.Rejected)
                    .Select(b => new { b.StartDate, b.EndDate })
                    .ToListAsync();
                windows.AddRange(bookings.Select(b => (b.StartDate, b.EndDate, "حجز مؤكد")));

                var blocks = await _context.AvailabilityBlocks
                    .Where(a => a.ItemId == id && a.EndDate >= today)
                    .Select(a => new { a.StartDate, a.EndDate })
                    .ToListAsync();
                windows.AddRange(blocks.Select(a => (a.StartDate, a.EndDate, "حجز من المالك")));
            }

            var ordered = windows.OrderBy(w => w.Start).ToList();
            ViewBag.UnavailableWindows = ordered
                .Select(w => $"{w.Source}: من {w.Start:dd/MM/yyyy} إلى {w.End:dd/MM/yyyy}")
                .ToList();
            ViewBag.AvailabilityStatus = item.ApprovalStatus == ListingApprovalStatus.Pending
                ? "قيد مراجعة الإدارة — غير منشور في السوق بعد"
                : item.ApprovalStatus == ListingApprovalStatus.Rejected
                    ? "مرفوض من الإدارة — غير منشور في السوق"
                    : !item.IsActive
                        ? "غير متاح (أزاله المالك من السوق)"
                        : ordered.Any(w => w.Start <= today && w.End >= today)
                            ? "محجوز حالياً"
                            : ordered.Any()
                                ? "متاح — توجد فترات محجوزة"
                                : "متاح فوراً";

            ViewBag.ItemReviews = await _context.Reviews
                .Where(r => r.ItemId == id)
                .Include(r => r.Reviewer)
                .ToListAsync();

            // Every showcase card is backed by a database row; the null case only
            // keeps a stale link from crashing the page.
            return View(item);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!await CanCreateListingsAsync())
            {
                TempData["VerificationInfo"] = "عرض الأغراض متاح للمالكين المعتمدين فقط. أرسل طلب ترقية إلى الإدارة أولاً.";
                return RedirectToAction("OwnerRequest", "Account");
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(new Item());
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Item item, List<IFormFile> angleImages)
        {
            if (!await CanCreateListingsAsync())
            {
                TempData["VerificationInfo"] = "عرض الأغراض متاح للمالكين المعتمدين فقط. أرسل طلب ترقية إلى الإدارة أولاً.";
                return RedirectToAction("OwnerRequest", "Account");
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Challenge();

            item.OwnerId = userId;
            // The item stays off the market until an admin verifies the product
            // itself, its six clear photos, its condition and its quality.
            item.IsActive = false;
            item.ApprovalStatus = ListingApprovalStatus.Pending;

            // Navigation/collection properties are populated by EF, not by the form.
            // Remove their implicit validation errors before checking ModelState.
            foreach (var key in new[] { "OwnerId", "Owner", "Category", "ItemImages", "AvailabilityBlocks", "Bookings", "Reviews" })
                ModelState.Remove(key);

            if (angleImages == null || angleImages.Count < 6 || angleImages.Any(f => f == null || f.Length == 0))
            {
                ModelState.AddModelError("", "يرجى رفع 6 صور للفحص سداسي الزوايا قبل النشر.");
            }

            if (!await _context.Categories.AnyAsync(c => c.Id == item.CategoryId))
            {
                ModelState.AddModelError(nameof(item.CategoryId), "يرجى اختيار قسم صحيح.");
            }

            if (!ModelState.IsValid)
                return View(item);

            var savedPaths = new List<string>();
            var folder = Path.Combine(_environment.WebRootPath, "uploads", "items", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);

            try
            {
                for (var i = 0; i < 6; i++)
                {
                    var file = angleImages[i];
                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp"))
                        extension = ".jpg";

                    var fileName = $"angle-{i + 1}{extension}";
                    var fullPath = Path.Combine(folder, fileName);
                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    savedPaths.Add($"/uploads/items/{Path.GetFileName(folder)}/{fileName}");
                }
            }
            catch (Exception)
            {
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
                ModelState.AddModelError("", "تعذر حفظ الصور. حاول مرة أخرى.");
                return View(item);
            }

            _context.Items.Add(item);
            await _context.SaveChangesAsync();

            for (var i = 0; i < savedPaths.Count; i++)
            {
                _context.ItemImages.Add(new ItemImage
                {
                    ItemId = item.Id,
                    AngleIndex = i + 1,
                    ImagePath = savedPaths[i]
                });
            }
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم إرسال الغرض إلى الإدارة للمراجعة. سيظهر في السوق بعد التأكد من وجود المنتج وجودة الصور الست وحالته.";
            return RedirectToAction("MyListings", "Account");
        }

        private async Task PopulateEditViewDataAsync(int itemId)
        {
            ViewBag.Categories = await _context.Categories
                .OrderBy(c => c.Id)
                .ToListAsync();
            ViewBag.ExistingImages = await _context.ItemImages
                .AsNoTracking()
                .Where(img => img.ItemId == itemId)
                .OrderBy(img => img.AngleIndex)
                .ToListAsync();
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var item = await _context.Items
                .FirstOrDefaultAsync(i => i.Id == id && i.OwnerId == userId);
            if (item == null) return NotFound();

            await PopulateEditViewDataAsync(id);
            return View(item);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, List<IFormFile>? angleImages)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var item = await _context.Items
                .FirstOrDefaultAsync(i => i.Id == id && i.OwnerId == userId);
            if (item == null) return NotFound();

            // Bind only the fields an owner is allowed to change. Approval status,
            // IsActive, owner and all admin inspection flags stay untouched.
            await TryUpdateModelAsync(
                item,
                string.Empty,
                i => i.Title,
                i => i.Description,
                i => i.CategoryId,
                i => i.City,
                i => i.PricePerDay,
                i => i.SecurityDeposit,
                i => i.Condition);

            foreach (var key in new[] { "Owner", "Category", "ItemImages", "AvailabilityBlocks", "Bookings", "Reviews" })
                ModelState.Remove(key);

            if (!await _context.Categories.AnyAsync(c => c.Id == item.CategoryId))
                ModelState.AddModelError(nameof(item.CategoryId), "يرجى اختيار قسم صحيح.");

            if (!ModelState.IsValid)
            {
                await PopulateEditViewDataAsync(id);
                return View(item);
            }

            var existingImages = await _context.ItemImages
                .Where(img => img.ItemId == id)
                .OrderBy(img => img.AngleIndex)
                .ToListAsync();

            // Each of the six angle inputs is optional. When supplied, the new
            // photo replaces the existing photo for that same angle.
            var uploads = new IFormFile?[6];
            if (angleImages != null)
            {
                for (var i = 0; i < Math.Min(6, angleImages.Count); i++)
                {
                    if (angleImages[i] != null && angleImages[i].Length > 0)
                        uploads[i] = angleImages[i];
                }
            }

            var savedFiles = new List<(int Angle, string RelativePath)>();
            if (uploads.Any(f => f != null))
            {
                var folder = Path.Combine(_environment.WebRootPath, "uploads", "items", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(folder);

                try
                {
                    for (var i = 0; i < uploads.Length; i++)
                    {
                        var file = uploads[i];
                        if (file == null) continue;

                        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                        if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp"))
                            extension = ".jpg";

                        var fileName = $"angle-{i + 1}{extension}";
                        var fullPath = Path.Combine(folder, fileName);
                        using (var stream = new FileStream(fullPath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        savedFiles.Add((i + 1, $"/uploads/items/{Path.GetFileName(folder)}/{fileName}"));
                    }
                }
                catch (Exception)
                {
                    if (Directory.Exists(folder)) Directory.Delete(folder, true);
                    ModelState.AddModelError("", "تعذر حفظ الصور الجديدة. حاول مرة أخرى.");
                    await PopulateEditViewDataAsync(id);
                    return View(item);
                }
            }

            var replacedPaths = new List<string>();
            foreach (var saved in savedFiles)
            {
                var existing = existingImages.FirstOrDefault(img => img.AngleIndex == saved.Angle);
                if (existing != null)
                {
                    if (!string.IsNullOrWhiteSpace(existing.ImagePath))
                        replacedPaths.Add(existing.ImagePath);
                    existing.ImagePath = saved.RelativePath;
                }
                else
                {
                    _context.ItemImages.Add(new ItemImage
                    {
                        ItemId = id,
                        AngleIndex = saved.Angle,
                        ImagePath = saved.RelativePath
                    });
                }
            }

            await _context.SaveChangesAsync();

            // Remove the old files only after the database update succeeded.
            foreach (var oldPath in replacedPaths)
            {
                if (!oldPath.StartsWith("/uploads/items/")) continue;
                var fullPath = Path.Combine(
                    _environment.WebRootPath,
                    oldPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
            }

            TempData["Success"] = "تم حفظ التعديلات بنجاح.";
            return RedirectToAction("MyListings", "Account");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ManageAvailability(int itemId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var item = await _context.Items
                .FirstOrDefaultAsync(i => i.Id == itemId && i.OwnerId == userId);
            if (item == null) return NotFound();

            ViewBag.AvailabilityBlocks = await _context.AvailabilityBlocks
                .Where(a => a.ItemId == itemId)
                .OrderBy(a => a.StartDate)
                .ToListAsync();
            ViewBag.ActiveBookings = await _context.Bookings
                .Where(b => b.ItemId == itemId
                    && (b.Status == BookingStatus.Approved || b.Status == BookingStatus.Active))
                .OrderBy(b => b.StartDate)
                .ToListAsync();

            return View(item);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBlock(int itemId, DateTime startDate, DateTime endDate, string? reason)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var item = await _context.Items
                .FirstOrDefaultAsync(i => i.Id == itemId && i.OwnerId == userId);
            if (item == null) return NotFound();

            if (endDate.Date < startDate.Date)
            {
                TempData["Error"] = "تاريخ النهاية يجب أن يكون بعد أو مساوياً لتاريخ البداية.";
            }
            else
            {
                var existingBlocks = await _context.AvailabilityBlocks
                    .Where(a => a.ItemId == itemId)
                    .Select(a => new { a.StartDate, a.EndDate })
                    .ToListAsync();
                var activeBookings = await _context.Bookings
                    .Where(b => b.ItemId == itemId
                        && (b.Status == BookingStatus.Approved || b.Status == BookingStatus.Active))
                    .Select(b => new { b.StartDate, b.EndDate })
                    .ToListAsync();

                var overlaps = existingBlocks.Any(a =>
                        BusinessRules.DatesOverlap(startDate, endDate, a.StartDate, a.EndDate))
                    || activeBookings.Any(b =>
                        BusinessRules.DatesOverlap(startDate, endDate, b.StartDate, b.EndDate));

                if (overlaps)
                {
                    TempData["Error"] = "تتداخل هذه الفترة مع فترة محجوزة مسبقاً أو مع حجز مؤكد. اختر نافذة أخرى.";
                }
                else
                {
                    _context.AvailabilityBlocks.Add(new AvailabilityBlock
                    {
                        ItemId = itemId,
                        StartDate = startDate.Date,
                        EndDate = endDate.Date,
                        IsBooked = false
                    });
                    await _context.SaveChangesAsync();

                    var note = string.IsNullOrWhiteSpace(reason) ? string.Empty : $" ({reason})";
                    TempData["Success"] = $"تمت إضافة فترة التوفر{note} من {startDate:dd/MM/yyyy} إلى {endDate:dd/MM/yyyy}.";
                }
            }

            return RedirectToAction(nameof(ManageAvailability), new { itemId });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveBlock(int itemId, int blockId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ownsItem = await _context.Items
                .AnyAsync(i => i.Id == itemId && i.OwnerId == userId);
            if (!ownsItem) return NotFound();

            var block = await _context.AvailabilityBlocks
                .FirstOrDefaultAsync(a => a.Id == blockId && a.ItemId == itemId);
            if (block == null) return NotFound();

            _context.AvailabilityBlocks.Remove(block);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تمت إزالة فترة التوفر.";
            return RedirectToAction(nameof(ManageAvailability), new { itemId });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == id && i.OwnerId == userId);
            if (item == null) return NotFound();

            if (item.ApprovalStatus != ListingApprovalStatus.Approved)
            {
                TempData["Error"] = "لا يمكن نشر الغرض قبل موافقة الإدارة على الفحص والصور الست.";
                return RedirectToAction("MyListings", "Account");
            }

            item.IsActive = !item.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = item.IsActive
                ? "تمت إعادة الغرض إلى السوق."
                : "تم إزالة الغرض من السوق ولن يظهر للمستأجرين.";
            return RedirectToAction("MyListings", "Account");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResubmitForReview(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == id && i.OwnerId == userId);
            if (item == null) return NotFound();

            if (item.ApprovalStatus != ListingApprovalStatus.Rejected)
            {
                TempData["Error"] = "يمكن إعادة الإرسال للأغراض المرفوضة فقط.";
                return RedirectToAction("MyListings", "Account");
            }

            item.ApprovalStatus = ListingApprovalStatus.Pending;
            item.IsActive = false;
            item.ReviewedAt = null;
            item.AdminNote = null;
            item.PhysicallyVerified = false;
            item.HasSixClearPhotos = false;
            item.IsUndamaged = false;
            item.QualityAccepted = false;
            await _context.SaveChangesAsync();

            TempData["Success"] = "تمت إعادة إرسال الغرض لمراجعة الإدارة.";
            return RedirectToAction("MyListings", "Account");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var item = await _context.Items
                .Include(i => i.ItemImages)
                .FirstOrDefaultAsync(i => i.Id == id && i.OwnerId == userId);
            if (item == null) return NotFound();

            if (await _context.Bookings.AnyAsync(b => b.ItemId == id))
            {
                TempData["Error"] = "لا يمكن حذف الغرض لوجود حجوزات مرتبطة به. يمكنك إزالته من السوق بدلاً من ذلك.";
                return RedirectToAction("MyListings", "Account");
            }

            // Keep the paths before EF removes the tracked image entities.
            var imagePaths = item.ItemImages?.Select(i => i.ImagePath).ToList() ?? new List<string>();

            _context.FavoriteItems.RemoveRange(_context.FavoriteItems.Where(f => f.ItemId == id));
            _context.AvailabilityBlocks.RemoveRange(_context.AvailabilityBlocks.Where(a => a.ItemId == id));
            _context.Reviews.RemoveRange(_context.Reviews.Where(r => r.ItemId == id));
            _context.ItemImages.RemoveRange(item.ItemImages);
            _context.Items.Remove(item);
            await _context.SaveChangesAsync();

            // Delete the uploaded image files left behind on disk.
            var folders = new HashSet<string>();
            foreach (var path in imagePaths)
            {
                if (string.IsNullOrWhiteSpace(path) || !path.StartsWith("/uploads/items/")) continue;

                var fullPath = Path.Combine(_environment.WebRootPath, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);

                var folder = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(folder)) folders.Add(folder);
            }
            foreach (var folder in folders)
            {
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
            }

            TempData["Success"] = "تم حذف الغرض نهائياً.";
            return RedirectToAction("MyListings", "Account");
        }
    }
}
