using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalefnyMVC.Data;
using SalefnyMVC.Models.Entities;
using SalefnyMVC.Models.Enums;
using SalefnyMVC.Models.ViewModels;
using SalefnyMVC.ViewModels;
using SalefnyMVC.Services;
using System.Text;
using System.Text.Json;

namespace Salefny.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly SalefnyDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notifications;
        private readonly IEscrowService _escrow;

        public AdminController(
            SalefnyDbContext context,
            UserManager<ApplicationUser> userManager,
            INotificationService notifications,
            IEscrowService escrow)
        {
            _context = context;
            _userManager = userManager;
            _notifications = notifications;
            _escrow = escrow;
        }

        private string? CurrentAdminId => _userManager.GetUserId(User);

        // ------------------------------------------------------------------
        // Dashboard
        // ------------------------------------------------------------------
        public async Task<IActionResult> Dashboard()
        {
            ViewData["AdminActive"] = "Dashboard";
            ViewData["Title"] = "لوحة تحكم سَلِّفني - النظرة العامة";

            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthEnd = monthStart.AddMonths(1);

            var users = await _context.Users.AsNoTracking().ToListAsync();
            var ownerIds = await (from ur in _context.UserRoles
                                  join r in _context.Roles on ur.RoleId equals r.Id
                                  where r.Name == AppRoles.Owner
                                  select ur.UserId).Distinct().ToListAsync();

            var items = await _context.Items.AsNoTracking().ToListAsync();
            var categories = await _context.Categories.AsNoTracking().ToListAsync();
            var bookings = await _context.Bookings.AsNoTracking().ToListAsync();

            var model = new AdminDashboardViewModel
            {
                TotalUsers = users.Count,
                VerifiedUsers = users.Count(u => u.IsVerified),
                Owners = ownerIds.Count,
                ActiveListings = items.Count(i => i.IsActive && i.ApprovalStatus == ListingApprovalStatus.Approved),
                PendingListings = items.Count(i => i.ApprovalStatus == ListingApprovalStatus.Pending),
                PendingOwnerRequests = await _context.OwnerRequests.CountAsync(r => r.Status == OwnerRequestStatus.Pending),
                PendingVerificationsCount = await _context.IdentityVerifications.CountAsync(v => v.Status == VerificationStatus.Pending),
                ActiveDisputesCount = await _context.DisputeLogs.CountAsync(d => !d.IsResolved),
                BookingsByStatus = bookings.GroupBy(b => b.Status).ToDictionary(g => g.Key, g => g.Count()),
                ThisMonthBookings = bookings.Count(b => b.StartDate >= monthStart && b.StartDate < monthEnd),
                RevenueEstimate = bookings
                    .Where(b => b.Status == BookingStatus.Approved
                             || b.Status == BookingStatus.Active
                             || b.Status == BookingStatus.Completed)
                    .Sum(b => b.TotalRentalAmount),
                EscrowHeld = bookings.Where(b => b.DepositStatus == DepositStatus.Held).Sum(b => b.EscrowDepositAmount),
                EscrowFrozen = bookings.Where(b => b.DepositStatus == DepositStatus.Frozen).Sum(b => b.EscrowDepositAmount)
            };

            // Top 5 categories by number of bookings on their items.
            var itemCategory = items.ToDictionary(i => i.Id, i => i.CategoryId);
            model.TopCategories = bookings
                .Where(b => itemCategory.ContainsKey(b.ItemId))
                .GroupBy(b => itemCategory[b.ItemId])
                .Select(g => new CategoryBookingStat
                {
                    CategoryId = g.Key,
                    CategoryName = categories.FirstOrDefault(c => c.Id == g.Key)?.Name ?? "غير مصنّف",
                    BookingCount = g.Count()
                })
                .OrderByDescending(c => c.BookingCount)
                .ThenBy(c => c.CategoryName)
                .Take(5)
                .ToList();

            // Last six calendar months (oldest first) for the CSS bar chart.
            for (var offset = 5; offset >= 0; offset--)
            {
                var month = new DateTime(now.Year, now.Month, 1).AddMonths(-offset);
                var nextMonth = month.AddMonths(1);
                model.BookingsPerMonth.Add(new MonthlyBookingStat
                {
                    Year = month.Year,
                    Month = month.Month,
                    Label = $"{month.Year}/{month.Month:00}",
                    Count = bookings.Count(b => b.StartDate >= month && b.StartDate < nextMonth)
                });
            }

            return View(model);
        }

        // ------------------------------------------------------------------
        // Users
        // ------------------------------------------------------------------
        public async Task<IActionResult> Users(string? search)
        {
            ViewData["AdminActive"] = "Users";
            ViewData["Title"] = "إدارة المستخدمين والصلاحيات";

            var currentUserId = CurrentAdminId;
            var query = _context.Users.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(u =>
                    (u.Email != null && u.Email.Contains(term)) ||
                    (u.UserName != null && u.UserName.Contains(term)) ||
                    u.FullName.Contains(term));
            }

            var users = await query.OrderBy(u => u.FullName).ToListAsync();
            var rows = new List<AdminUserRow>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                rows.Add(new AdminUserRow
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    City = user.City,
                    IsVerified = user.IsVerified,
                    IsActive = user.IsActive,
                    Roles = roles.ToList(),
                    ListingsCount = await _context.Items.CountAsync(i => i.OwnerId == user.Id),
                    BookingsCount = await _context.Bookings.CountAsync(b => b.RenterId == user.Id),
                    IsSuperAdmin = roles.Contains(AppRoles.SuperAdmin),
                    IsSelf = user.Id == currentUserId
                });
            }

            return View(new AdminUsersViewModel
            {
                Search = search ?? string.Empty,
                Users = rows
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuspendUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["AdminError"] = "المستخدم غير موجود.";
                return RedirectToAction(nameof(Users));
            }

            if (user.Id == CurrentAdminId)
            {
                TempData["AdminError"] = "لا يمكنك إيقاف حسابك الحالي.";
                return RedirectToAction(nameof(Users));
            }

            if (await _userManager.IsInRoleAsync(user, AppRoles.SuperAdmin))
            {
                TempData["AdminError"] = "لا يمكن إيقاف حساب مدير عام.";
                return RedirectToAction(nameof(Users));
            }

            user.IsActive = false;
            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
            var update = await _userManager.UpdateAsync(user);
            if (!update.Succeeded)
            {
                TempData["AdminError"] = "تعذّر إيقاف الحساب: " +
                    string.Join("، ", update.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Users));
            }

            // Any open booking where the suspended user is either party is cancelled.
            var openBookings = await _context.Bookings
                .Where(b => (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Approved)
                         && (b.RenterId == id || b.Item.OwnerId == id))
                .ToListAsync();

            foreach (var booking in openBookings)
            {
                booking.Status = BookingStatus.Cancelled;
                booking.CancellationReason = "أُلغي الحجز تلقائياً لإيقاف حساب أحد أطرافه من قِبل الإدارة.";
                booking.UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();

            await _notifications.NotifyAsync(
                user.Id,
                "تم إيقاف حسابك من قِبل إدارة سَلِّفني. يُرجى التواصل مع الدعم لمعرفة السبب.",
                "account",
                "/Account/Login");

            TempData["AdminMessage"] = $"تم إيقاف حساب {user.Email} وإلغاء {openBookings.Count} حجز مفتوح.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["AdminError"] = "المستخدم غير موجود.";
                return RedirectToAction(nameof(Users));
            }

            user.IsActive = true;
            user.LockoutEnabled = false;
            user.LockoutEnd = null;
            user.AccessFailedCount = 0;
            var update = await _userManager.UpdateAsync(user);
            if (!update.Succeeded)
            {
                TempData["AdminError"] = "تعذّر تفعيل الحساب: " +
                    string.Join("، ", update.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Users));
            }

            await _notifications.NotifyAsync(
                user.Id,
                "تمت إعادة تفعيل حسابك في سَلِّفني. يمكنك تسجيل الدخول والمتابعة.",
                "account",
                "/Account/Login");

            TempData["AdminMessage"] = $"تمت إعادة تفعيل حساب {user.Email}.";
            return RedirectToAction(nameof(Users));
        }

        // ------------------------------------------------------------------
        // Categories
        // ------------------------------------------------------------------
        public async Task<IActionResult> Categories()
        {
            ViewData["AdminActive"] = "Categories";
            ViewData["Title"] = "إدارة تصنيفات الأغراض";

            var categories = await _context.Categories
                .Include(c => c.ParentCategory)
                .Include(c => c.SubCategories)
                .Include(c => c.Items)
                .OrderBy(c => c.ParentCategoryId ?? 0)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var model = new AdminCategoriesViewModel
            {
                Categories = categories.Select(c => new AdminCategoryRow
                {
                    Category = c,
                    ParentName = c.ParentCategory?.Name,
                    ItemsCount = c.Items.Count,
                    SubCategoriesCount = c.SubCategories.Count
                }).ToList(),
                ParentOptions = categories.OrderBy(c => c.Name).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory([Bind(Prefix = "CreateForm")] AdminCategoryForm form)
        {
            if (!ModelState.IsValid)
            {
                TempData["AdminError"] = ModelState.Values.SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage).FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
                    ?? "بيانات التصنيف غير صحيحة.";
                return RedirectToAction(nameof(Categories));
            }

            var name = form.Name.Trim();
            if (await _context.Categories.AnyAsync(c => c.Name == name))
            {
                TempData["AdminError"] = "يوجد تصنيف بهذا الاسم بالفعل.";
                return RedirectToAction(nameof(Categories));
            }

            if (form.ParentCategoryId.HasValue &&
                !await _context.Categories.AnyAsync(c => c.Id == form.ParentCategoryId.Value))
            {
                TempData["AdminError"] = "التصنيف الرئيسي المحدد غير موجود.";
                return RedirectToAction(nameof(Categories));
            }

            try
            {
                _context.Categories.Add(new Category
                {
                    Name = name,
                    Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim(),
                    IconClass = string.IsNullOrWhiteSpace(form.IconClass) ? null : form.IconClass.Trim(),
                    ParentCategoryId = form.ParentCategoryId,
                    IsActive = form.IsActive
                });
                await _context.SaveChangesAsync();
                TempData["AdminMessage"] = $"تم إنشاء التصنيف «{name}».";
            }
            catch (DbUpdateException)
            {
                TempData["AdminError"] = "تعذّر إنشاء التصنيف. تأكد من عدم تكرار الاسم.";
            }

            return RedirectToAction(nameof(Categories));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(int id, AdminCategoryForm form)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                TempData["AdminError"] = "التصنيف غير موجود.";
                return RedirectToAction(nameof(Categories));
            }

            if (!ModelState.IsValid)
            {
                TempData["AdminError"] = ModelState.Values.SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage).FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
                    ?? "بيانات التصنيف غير صحيحة.";
                return RedirectToAction(nameof(Categories));
            }

            var name = form.Name.Trim();
            if (await _context.Categories.AnyAsync(c => c.Name == name && c.Id != id))
            {
                TempData["AdminError"] = "يوجد تصنيف بهذا الاسم بالفعل.";
                return RedirectToAction(nameof(Categories));
            }

            if (form.ParentCategoryId == id)
            {
                TempData["AdminError"] = "لا يمكن أن يكون التصنيف رئيسياً لنفسه.";
                return RedirectToAction(nameof(Categories));
            }

            if (form.ParentCategoryId.HasValue &&
                !await _context.Categories.AnyAsync(c => c.Id == form.ParentCategoryId.Value))
            {
                TempData["AdminError"] = "التصنيف الرئيسي المحدد غير موجود.";
                return RedirectToAction(nameof(Categories));
            }

            category.Name = name;
            category.Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim();
            category.IconClass = string.IsNullOrWhiteSpace(form.IconClass) ? null : form.IconClass.Trim();
            category.ParentCategoryId = form.ParentCategoryId;
            category.IsActive = form.IsActive;

            try
            {
                await _context.SaveChangesAsync();
                TempData["AdminMessage"] = $"تم تحديث التصنيف «{name}».";
            }
            catch (DbUpdateException)
            {
                TempData["AdminError"] = "تعذّر حفظ التعديلات. تأكد من عدم تكرار الاسم.";
            }

            return RedirectToAction(nameof(Categories));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCategoryActive(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                TempData["AdminError"] = "التصنيف غير موجود.";
                return RedirectToAction(nameof(Categories));
            }

            category.IsActive = !category.IsActive;
            await _context.SaveChangesAsync();
            TempData["AdminMessage"] = category.IsActive
                ? $"تم تفعيل التصنيف «{category.Name}»."
                : $"تم إيقاف التصنيف «{category.Name}».";

            return RedirectToAction(nameof(Categories));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                TempData["AdminError"] = "التصنيف غير موجود.";
                return RedirectToAction(nameof(Categories));
            }

            if (await _context.Items.AnyAsync(i => i.CategoryId == id) ||
                await _context.Categories.AnyAsync(c => c.ParentCategoryId == id) ||
                await _context.OwnerRequests.AnyAsync(r => r.CategoryId == id))
            {
                TempData["AdminError"] = "لا يمكن حذف تصنيف يحتوي على أغراض أو طلبات مالكين أو تصنيفات فرعية.";
                return RedirectToAction(nameof(Categories));
            }

            try
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                TempData["AdminMessage"] = $"تم حذف التصنيف «{category.Name}».";
            }
            catch (DbUpdateException)
            {
                TempData["AdminError"] = "تعذّر حذف التصنيف لارتباطه ببيانات أخرى.";
            }

            return RedirectToAction(nameof(Categories));
        }

        // ------------------------------------------------------------------
        // Identity verifications
        // ------------------------------------------------------------------
        public async Task<IActionResult> Verifications()
        {
            ViewData["AdminActive"] = "Verifications";
            ViewData["Title"] = "قائمة توثيق الهوية الوطنية (eKYC)";

            var verifications = await _context.IdentityVerifications
                .Include(v => v.User)
                .OrderBy(v => v.Status == VerificationStatus.Pending ? 0 : 1)
                .ThenByDescending(v => v.SubmittedAt)
                .ToListAsync();

            return View(new AdminVerificationsViewModel { Verifications = verifications });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveVerification(int id, string? adminNote)
        {
            var verification = await _context.IdentityVerifications
                .Include(v => v.User)
                .FirstOrDefaultAsync(v => v.Id == id && v.Status == VerificationStatus.Pending);

            if (verification == null)
            {
                TempData["AdminError"] = "لم يتم العثور على طلب توثيق قيد المراجعة.";
                return RedirectToAction(nameof(Verifications));
            }

            verification.Status = VerificationStatus.Approved;
            verification.AdminNote = string.IsNullOrWhiteSpace(adminNote)
                ? "تمت الموافقة على بيانات الهوية بعد المراجعة."
                : adminNote.Trim();
            verification.ReviewedByAdminId = CurrentAdminId;
            verification.ReviewedAt = DateTime.UtcNow;
            if (verification.User != null)
                verification.User.IsVerified = true;

            await _context.SaveChangesAsync();

            await _notifications.NotifyAsync(
                verification.UserId,
                "تمت الموافقة على توثيق هويتك في سَلِّفني. أصبح حسابك موثّقاً.",
                "verification",
                "/Account/Profile");

            TempData["AdminMessage"] = "تمت الموافقة على طلب التوثيق وتحديث حالة المستخدم.";
            return RedirectToAction(nameof(Verifications));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectVerification(int id, string? adminNote)
        {
            var verification = await _context.IdentityVerifications
                .Include(v => v.User)
                .FirstOrDefaultAsync(v => v.Id == id && v.Status == VerificationStatus.Pending);

            if (verification == null)
            {
                TempData["AdminError"] = "لم يتم العثور على طلب توثيق قيد المراجعة.";
                return RedirectToAction(nameof(Verifications));
            }

            verification.Status = VerificationStatus.Rejected;
            verification.AdminNote = string.IsNullOrWhiteSpace(adminNote)
                ? "تم رفض التوثيق لعدم مطابقة البيانات أو الصور."
                : adminNote.Trim();
            verification.ReviewedByAdminId = CurrentAdminId;
            verification.ReviewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _notifications.NotifyAsync(
                verification.UserId,
                $"تم رفض طلب توثيق الهوية. السبب: {verification.AdminNote}",
                "verification",
                "/Verification/Verify");

            TempData["AdminMessage"] = "تم رفض طلب التوثيق وإخطار المستخدم.";
            return RedirectToAction(nameof(Verifications));
        }

        // ------------------------------------------------------------------
        // Disputes
        // ------------------------------------------------------------------
        public async Task<IActionResult> Disputes(bool showResolved = false)
        {
            ViewData["AdminActive"] = "Disputes";
            ViewData["Title"] = "منازعات فحص الـ 6 زوايا وتسوية الأضرار";

            var query = _context.DisputeLogs
                .Include(d => d.Booking).ThenInclude(b => b.Item).ThenInclude(i => i.Owner)
                .Include(d => d.Booking).ThenInclude(b => b.Item).ThenInclude(i => i.ItemImages)
                .Include(d => d.Booking).ThenInclude(b => b.Renter)
                .AsQueryable();

            if (!showResolved)
                query = query.Where(d => !d.IsResolved);

            var disputes = await query.OrderByDescending(d => d.LoggedAt).ToListAsync();

            var rows = disputes.Select(d =>
            {
                var booking = d.Booking;
                var item = booking?.Item;
                return new AdminDisputeRow
                {
                    Id = d.Id,
                    ClaimDescription = d.ClaimDescription,
                    IsResolved = d.IsResolved,
                    DeductedDepositAmount = d.DeductedDepositAmount,
                    ResolutionNote = d.ResolutionNote,
                    ResolvedAt = d.ResolvedAt,
                    LoggedAt = d.LoggedAt,
                    ReturnPhotos = ParsePhotoPaths(d.ReturnAnglePhotoPaths),
                    BookingId = booking?.Id ?? 0,
                    BookingStatus = booking?.Status ?? BookingStatus.Pending,
                    DepositStatus = booking?.DepositStatus ?? DepositStatus.NotHeld,
                    EscrowDepositAmount = booking?.EscrowDepositAmount ?? 0,
                    StartDate = booking?.StartDate ?? default,
                    EndDate = booking?.EndDate ?? default,
                    ItemId = item?.Id ?? 0,
                    ItemTitle = item?.Title ?? "غرض محذوف",
                    ItemImagePath = item?.ItemImages?.OrderBy(i => i.AngleIndex).FirstOrDefault()?.ImagePath,
                    RenterId = booking?.RenterId ?? string.Empty,
                    RenterName = booking?.Renter?.FullName ?? "—",
                    RenterEmail = booking?.Renter?.Email ?? string.Empty,
                    OwnerId = item?.OwnerId ?? string.Empty,
                    OwnerName = item?.Owner?.FullName ?? "—",
                    OwnerEmail = item?.Owner?.Email ?? string.Empty
                };
            }).ToList();

            return View(new AdminDisputesViewModel
            {
                Disputes = rows,
                ShowResolved = showResolved,
                PendingCount = await _context.DisputeLogs.CountAsync(d => !d.IsResolved)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveDispute(
            int id, bool refundFull, decimal deductedAmount, string? resolutionNote)
        {
            var dispute = await _context.DisputeLogs
                .Include(d => d.Booking).ThenInclude(b => b.Item).ThenInclude(i => i.Owner)
                .Include(d => d.Booking).ThenInclude(b => b.Renter)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (dispute == null || dispute.IsResolved || dispute.Booking == null || dispute.Booking.Item == null)
            {
                TempData["AdminError"] = "النزاع غير موجود أو تمت تسويته بالفعل.";
                return RedirectToAction(nameof(Disputes));
            }

            var booking = dispute.Booking;
            var deduction = refundFull
                ? 0
                : Math.Min(Math.Max(deductedAmount, 0), booking.EscrowDepositAmount);

            dispute.IsResolved = true;
            dispute.ResolvedAt = DateTime.UtcNow;
            dispute.ResolvedByAdminId = CurrentAdminId;
            dispute.DeductedDepositAmount = deduction;
            dispute.ResolutionNote = string.IsNullOrWhiteSpace(resolutionNote)
                ? (refundFull
                    ? "رفض الادعاء وإرجاع كامل مبلغ التأمين للمستأجر."
                    : $"تم خصم {deduction:N2} ج.م من مبلغ التأمين وإرجاع المتبقي.")
                : resolutionNote.Trim();

            booking.Status = BookingStatus.Completed;
            booking.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            if (deduction > 0)
                await _escrow.DeductAsync(booking.Id, deduction, CurrentAdminId, dispute.ResolutionNote);
            else
                await _escrow.RefundAsync(booking.Id, CurrentAdminId, dispute.ResolutionNote);

            await _notifications.NotifyAsync(
                booking.RenterId,
                $"تمت تسوية النزاع على الحجز رقم {booking.Id}. {dispute.ResolutionNote}",
                "dispute",
                "/Bookings/MyBookings");

            if (!string.IsNullOrWhiteSpace(booking.Item.OwnerId))
            {
                await _notifications.NotifyAsync(
                    booking.Item.OwnerId,
                    $"تمت تسوية النزاع على الحجز رقم {booking.Id}. {dispute.ResolutionNote}",
                    "dispute",
                    "/Bookings/OwnerRequests");
            }

            TempData["AdminMessage"] = "تمت تسوية النزاع، تحديث حالة الحجز، وتحويل مبلغ الضمان.";
            return RedirectToAction(nameof(Disputes));
        }

        private static List<string> ParsePhotoPaths(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return new List<string>();

            try
            {
                var parsed = JsonSerializer.Deserialize<List<string>>(raw);
                if (parsed != null && parsed.Count > 0)
                    return parsed.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()).ToList();
            }
            catch (JsonException)
            {
                // Legacy disputes store a comma-separated list instead of JSON.
            }

            return raw.Split(new[] { ',', ';', '\n', '[', ']', '"' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => p.Length > 2)
                .ToList();
        }

        // ------------------------------------------------------------------
        // Escrow ledger
        // ------------------------------------------------------------------
        public async Task<IActionResult> EscrowLedger(string? bookingId, EscrowTransactionType? type)
        {
            ViewData["AdminActive"] = "EscrowLedger";
            ViewData["Title"] = "سجل ودائع الضمان وحركات الضمان";

            return View(await BuildEscrowModelAsync(bookingId, type));
        }

        [HttpGet]
        public async Task<IActionResult> ExportEscrowCsv(string? bookingId, EscrowTransactionType? type)
        {
            var model = await BuildEscrowModelAsync(bookingId, type);
            var csv = new StringBuilder();
            csv.Append('\uFEFF'); // UTF-8 BOM so Excel renders Arabic correctly.
            csv.AppendLine("رقم المعاملة,رقم الحجز,رقم العملية,الغرض,المستأجر,النوع,المبلغ (ج.م),ملاحظة,تاريخ الإنشاء");

            foreach (var tx in model.Transactions)
            {
                csv.Append(tx.Id).Append(',')
                   .Append(tx.BookingId).Append(',')
                   .Append(CsvEscape(tx.Booking?.Id.ToString())).Append(',')
                   .Append(CsvEscape(tx.Booking?.Item?.Title)).Append(',')
                   .Append(CsvEscape(tx.Booking?.Renter?.FullName)).Append(',')
                   .Append(CsvEscape(EscrowTypeLabel(tx.Type))).Append(',')
                   .Append(tx.Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)).Append(',')
                   .Append(CsvEscape(tx.Note)).Append(',')
                   .Append(tx.CreatedAt.ToLocalTime().ToString("yyyy/MM/dd HH:mm"))
                   .AppendLine();
            }

            var bytes = new UTF8Encoding(false).GetBytes(csv.ToString());
            return File(bytes, "text/csv; charset=utf-8", $"salefny-escrow-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv");
        }

        private async Task<AdminEscrowViewModel> BuildEscrowModelAsync(string? bookingId, EscrowTransactionType? type)
        {
            var query = _context.EscrowTransactions
                .Include(e => e.Booking).ThenInclude(b => b.Item)
                .Include(e => e.Booking).ThenInclude(b => b.Renter)
                .AsQueryable();

            var hasInvalidBookingFilter = false;
            if (!string.IsNullOrWhiteSpace(bookingId))
            {
                if (int.TryParse(bookingId.Trim(), out var parsedId))
                    query = query.Where(e => e.BookingId == parsedId);
                else
                    hasInvalidBookingFilter = true;
            }

            if (type.HasValue)
                query = query.Where(e => e.Type == type.Value);

            var transactions = hasInvalidBookingFilter
                ? new List<EscrowTransaction>()
                : await query.OrderByDescending(e => e.CreatedAt).ThenByDescending(e => e.Id).ToListAsync();

            return new AdminEscrowViewModel
            {
                BookingIdFilter = bookingId ?? string.Empty,
                TypeFilter = type,
                Transactions = transactions,
                TotalAmount = transactions.Sum(e => e.Amount),
                HoldTotal = transactions.Where(e => e.Type == EscrowTransactionType.Hold).Sum(e => e.Amount),
                ReleaseTotal = transactions.Where(e => e.Type == EscrowTransactionType.Release).Sum(e => e.Amount),
                FreezeTotal = transactions.Where(e => e.Type == EscrowTransactionType.Freeze).Sum(e => e.Amount),
                DeductTotal = transactions.Where(e => e.Type == EscrowTransactionType.Deduct).Sum(e => e.Amount),
                RefundTotal = transactions.Where(e => e.Type == EscrowTransactionType.Refund).Sum(e => e.Amount)
            };
        }

        private static string CsvEscape(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string EscrowTypeLabel(EscrowTransactionType type) => type switch
        {
            EscrowTransactionType.Hold => "حجز",
            EscrowTransactionType.Release => "إرجاع",
            EscrowTransactionType.Freeze => "تجميد",
            EscrowTransactionType.Deduct => "خصم",
            EscrowTransactionType.Refund => "استرداد",
            _ => type.ToString()
        };

        // ------------------------------------------------------------------
        // Owner requests (kept intact)
        // ------------------------------------------------------------------
        public async Task<IActionResult> OwnerRequests()
        {
            ViewData["AdminActive"] = "OwnerRequests";
            ViewData["Title"] = "طلبات الترقية إلى مالك";
            var requests = await _context.OwnerRequests
                .Include(r => r.User)
                .Include(r => r.Category)
                .Include(r => r.Images)
                .OrderBy(r => r.Status == OwnerRequestStatus.Pending ? 0 : 1)
                .ThenByDescending(r => r.RequestedAt)
                .ToListAsync();

            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveOwner(
            int id,
            bool physicallyVerified,
            bool hasSixClearPhotos,
            bool isUndamaged,
            bool qualityAccepted,
            string? adminNote)
        {
            var request = await _context.OwnerRequests
                .Include(r => r.User)
                .Include(r => r.Images)
                .FirstOrDefaultAsync(r => r.Id == id && r.Status == OwnerRequestStatus.Pending);

            if (request?.User == null)
            {
                TempData["AdminError"] = "لم يتم العثور على طلب المالك.";
                return RedirectToAction(nameof(OwnerRequests));
            }

            if (!physicallyVerified || !hasSixClearPhotos || !isUndamaged || !qualityAccepted)
            {
                TempData["AdminError"] = "يجب تأكيد وجود المنتج، وضوح الصور الست، خلوه من التلف، وجودته قبل الموافقة.";
                return RedirectToAction(nameof(OwnerRequests));
            }

            var images = request.Images.OrderBy(i => i.AngleIndex).ToList();
            if (images.Count != 6)
            {
                TempData["AdminError"] = "الطلب لا يحتوي على الصور الست المطلوبة.";
                return RedirectToAction(nameof(OwnerRequests));
            }

            if (!await _userManager.IsInRoleAsync(request.User, AppRoles.Owner))
            {
                var roleResult = await _userManager.AddToRoleAsync(request.User, AppRoles.Owner);
                if (!roleResult.Succeeded)
                {
                    TempData["AdminError"] = "تعذّر منح صلاحية المالك: " +
                        string.Join("، ", roleResult.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(OwnerRequests));
                }
            }

            if (!await _userManager.IsInRoleAsync(request.User, AppRoles.Renter))
                await _userManager.AddToRoleAsync(request.User, AppRoles.Renter);

            var reviewNote = string.IsNullOrWhiteSpace(adminNote)
                ? "تمت الموافقة بعد فحص المنتج والصور الست وحالته وجودته."
                : adminNote.Trim();

            var item = new Item
            {
                Title = request.ProductTitle,
                Description = string.IsNullOrWhiteSpace(request.Description)
                    ? "منتج معتمد من إدارة سَلِّفني بعد فحص الحالة والصور الست."
                    : request.Description,
                PricePerDay = request.PricePerDay,
                SecurityDeposit = request.SecurityDeposit,
                City = string.IsNullOrWhiteSpace(request.City) ? request.User.City ?? string.Empty : request.City,
                OwnerId = request.UserId,
                CategoryId = request.CategoryId,
                IsActive = true,
                ApprovalStatus = ListingApprovalStatus.Approved,
                PhysicallyVerified = true,
                HasSixClearPhotos = true,
                IsUndamaged = true,
                QualityAccepted = true,
                ReviewedAt = DateTime.UtcNow,
                AdminNote = reviewNote,
                ItemImages = images.Select(i => new ItemImage
                {
                    AngleIndex = i.AngleIndex,
                    ImagePath = i.ImagePath
                }).ToList()
            };

            try
            {
                _context.Items.Add(item);
                await _context.SaveChangesAsync();

                if (!request.AvailableNow && request.AvailableFrom.HasValue && request.AvailableTo.HasValue)
                {
                    _context.AvailabilityBlocks.Add(new AvailabilityBlock
                    {
                        ItemId = item.Id,
                        StartDate = request.AvailableFrom.Value.Date,
                        EndDate = request.AvailableTo.Value.Date,
                        IsBooked = false
                    });
                    await _context.SaveChangesAsync();
                }

                request.Status = OwnerRequestStatus.Approved;
                request.ReviewedAt = DateTime.UtcNow;
                request.AdminNote = reviewNote;
                request.CreatedItemId = item.Id;
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                TempData["AdminError"] = "تعذّر إنشاء المنتج بعد الموافقة. راجع بيانات الطلب وحاول مرة أخرى.";
                return RedirectToAction(nameof(OwnerRequests));
            }

            TempData["AdminMessage"] = $"تمت الموافقة على {request.User.Email} ك مالك، ونُشر المنتج «{item.Title}» في السوق.";
            return RedirectToAction(nameof(OwnerRequests));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectOwner(int id, string? adminNote)
        {
            var request = await _context.OwnerRequests
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id && r.Status == OwnerRequestStatus.Pending);

            if (request == null)
            {
                TempData["AdminError"] = "لم يتم العثور على طلب المالك.";
                return RedirectToAction(nameof(OwnerRequests));
            }

            request.Status = OwnerRequestStatus.Rejected;
            request.ReviewedAt = DateTime.UtcNow;
            request.AdminNote = string.IsNullOrWhiteSpace(adminNote)
                ? "تم رفض المنتج والطلب. يمكن للمستخدم إعادة التقديم بمنتج وصور جديدة."
                : adminNote.Trim();
            await _context.SaveChangesAsync();

            TempData["AdminMessage"] = "تم رفض طلب المالك.";
            return RedirectToAction(nameof(OwnerRequests));
        }

        // ------------------------------------------------------------------
        // Listing reviews (kept intact)
        // ------------------------------------------------------------------
        public async Task<IActionResult> ListingReviews()
        {
            ViewData["AdminActive"] = "ListingReviews";
            ViewData["Title"] = "مراجعة الأغراض قبل النشر";
            var items = await _context.Items
                .Include(i => i.Owner)
                .Include(i => i.Category)
                .Include(i => i.ItemImages)
                .OrderBy(i => i.ApprovalStatus == ListingApprovalStatus.Pending ? 0 : 1)
                .ThenByDescending(i => i.Id)
                .ToListAsync();

            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveListing(
            int id,
            bool physicallyVerified,
            bool hasSixClearPhotos,
            bool isUndamaged,
            bool qualityAccepted,
            string? adminNote)
        {
            var item = await _context.Items.FirstOrDefaultAsync(
                i => i.Id == id && i.ApprovalStatus == ListingApprovalStatus.Pending);

            if (item == null)
            {
                TempData["AdminError"] = "لم يتم العثور على طلب مراجعة الغرض.";
                return RedirectToAction(nameof(ListingReviews));
            }

            if (!physicallyVerified || !hasSixClearPhotos || !isUndamaged || !qualityAccepted)
            {
                TempData["AdminError"] = "يجب تأكيد وجود المنتج، وضوح الصور الست، خلوه من التلف، وجودته قبل النشر.";
                return RedirectToAction(nameof(ListingReviews));
            }

            item.ApprovalStatus = ListingApprovalStatus.Approved;
            item.IsActive = true;
            item.ReviewedAt = DateTime.UtcNow;
            item.PhysicallyVerified = true;
            item.HasSixClearPhotos = true;
            item.IsUndamaged = true;
            item.QualityAccepted = true;
            item.AdminNote = string.IsNullOrWhiteSpace(adminNote)
                ? "تمت الموافقة بعد فحص المنتج والصور الست وحالته وجودته."
                : adminNote.Trim();
            await _context.SaveChangesAsync();

            TempData["AdminMessage"] = $"تم اعتماد الغرض «{item.Title}» ونشره في السوق.";
            return RedirectToAction(nameof(ListingReviews));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectListing(int id, string? adminNote)
        {
            var item = await _context.Items.FirstOrDefaultAsync(
                i => i.Id == id && i.ApprovalStatus == ListingApprovalStatus.Pending);

            if (item == null)
            {
                TempData["AdminError"] = "لم يتم العثور على طلب مراجعة الغرض.";
                return RedirectToAction(nameof(ListingReviews));
            }

            item.ApprovalStatus = ListingApprovalStatus.Rejected;
            item.IsActive = false;
            item.ReviewedAt = DateTime.UtcNow;
            item.AdminNote = string.IsNullOrWhiteSpace(adminNote)
                ? "تم رفض الغرض بعد فحص الإدارة."
                : adminNote.Trim();
            await _context.SaveChangesAsync();

            TempData["AdminMessage"] = $"تم رفض الغرض «{item.Title}».";
            return RedirectToAction(nameof(ListingReviews));
        }

        // ------------------------------------------------------------------
        // Admin management (kept intact)
        // ------------------------------------------------------------------
        [Authorize(Roles = AppRoles.SuperAdmin)]
        public async Task<IActionResult> ManageAdmins()
        {
            ViewData["AdminActive"] = "ManageAdmins";
            ViewData["Title"] = "إدارة المدراء والصلاحيات";

            var currentUserId = CurrentAdminId;
            var users = await _context.Users.OrderBy(u => u.Email).ToListAsync();
            var model = new List<AdminManagementViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                model.Add(new AdminManagementViewModel
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    IsAdmin = roles.Contains(AppRoles.Admin),
                    IsSuperAdmin = roles.Contains(AppRoles.SuperAdmin),
                    IsCurrentUser = user.Id == currentUserId
                });
            }

            return View(model);
        }

        [Authorize(Roles = AppRoles.SuperAdmin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoteToAdmin(string userId)
        {
            var target = await _userManager.FindByIdAsync(userId);
            if (target == null)
            {
                TempData["AdminError"] = "المستخدم غير موجود.";
                return RedirectToAction(nameof(ManageAdmins));
            }

            if (await _userManager.IsInRoleAsync(target, AppRoles.SuperAdmin))
            {
                TempData["AdminError"] = "هذا الحساب مدير عام بالفعل ولا يمكن تعديله.";
                return RedirectToAction(nameof(ManageAdmins));
            }

            if (!await _userManager.IsInRoleAsync(target, AppRoles.Admin))
            {
                var result = await _userManager.AddToRoleAsync(target, AppRoles.Admin);
                if (!result.Succeeded)
                {
                    TempData["AdminError"] = "تعذّر منح صلاحية المدير: " +
                        string.Join("، ", result.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(ManageAdmins));
                }
            }

            TempData["AdminMessage"] = $"أصبح {target.Email} مديراً للنظام.";
            return RedirectToAction(nameof(ManageAdmins));
        }

        [Authorize(Roles = AppRoles.SuperAdmin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DemoteFromAdmin(string userId)
        {
            if (userId == CurrentAdminId)
            {
                TempData["AdminError"] = "لا يمكنك إلغاء صلاحية المدير العام لحسابك الحالي.";
                return RedirectToAction(nameof(ManageAdmins));
            }

            var target = await _userManager.FindByIdAsync(userId);
            if (target == null)
            {
                TempData["AdminError"] = "المستخدم غير موجود.";
                return RedirectToAction(nameof(ManageAdmins));
            }

            if (await _userManager.IsInRoleAsync(target, AppRoles.SuperAdmin))
            {
                TempData["AdminError"] = "لا يمكن إلغاء صلاحية مدير عام آخر.";
                return RedirectToAction(nameof(ManageAdmins));
            }

            if (await _userManager.IsInRoleAsync(target, AppRoles.Admin))
            {
                var result = await _userManager.RemoveFromRoleAsync(target, AppRoles.Admin);
                if (!result.Succeeded)
                {
                    TempData["AdminError"] = "تعذّر إلغاء صلاحية المدير: " +
                        string.Join("، ", result.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(ManageAdmins));
                }
            }

            TempData["AdminMessage"] = $"تم سحب صلاحية المدير من {target.Email}.";
            return RedirectToAction(nameof(ManageAdmins));
        }
    }
}
