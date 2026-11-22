using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalefnyMVC.Data;
using SalefnyMVC.Models.Entities;
using SalefnyMVC.Models.Enums;
using SalefnyMVC.Services;
using System.Security.Claims;

namespace Salefny.Controllers
{
    [Authorize]
    public class BookingsController : Controller
    {
        private readonly SalefnyDbContext _context;
        private readonly INotificationService _notifications;
        private readonly IEscrowService _escrow;
        private static readonly string[] AllowedDeliveryMethods = { "Handover", "Courier" };
        private static readonly string[] AllowedPaymentMethods = { "InstaPay", "OPay", "VodafoneCash", "BankCard", "CashOnDelivery" };

        public BookingsController(
            SalefnyDbContext context,
            INotificationService notifications,
            IEscrowService escrow)
        {
            _context = context;
            _notifications = notifications;
            _escrow = escrow;
        }

        [HttpGet]
        public async Task<IActionResult> Create(int itemId, DateTime? startDate, DateTime? endDate)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = currentUserId == null ? null : await _context.Users.FindAsync(currentUserId);
            if (currentUser == null || !currentUser.IsVerified)
            {
                TempData["Error"] = "يجب توثيق هويتك أولاً قبل حجز أي غرض.";
                return RedirectToAction("Verify", "Verification");
            }
            var item = await _context.Items
                .Include(i => i.Owner)
                .Include(i => i.Category)
                .Include(i => i.ItemImages)
                .FirstOrDefaultAsync(i => i.Id == itemId);
            if (item == null) return NotFound();

            // Dates come from the booking window chosen on the item detail page.
            var start = startDate.HasValue && startDate.Value.Date >= DateTime.Today
                ? startDate.Value.Date
                : DateTime.Today.AddDays(1);
            var end = endDate.HasValue && endDate.Value.Date >= start
                ? endDate.Value.Date
                : start.AddDays(BusinessRules.MinBookingDays + 1);

            ViewBag.StartDate = start.ToString("yyyy-MM-dd");
            ViewBag.EndDate = end.ToString("yyyy-MM-dd");
            ViewBag.MaxStartDate = DateTime.Today.AddDays(BusinessRules.MaxAdvanceDays).ToString("yyyy-MM-dd");
            ViewBag.MaxEndDate = start.AddDays(BusinessRules.MaxBookingDays - 1).ToString("yyyy-MM-dd");
            ViewBag.DeliveryMethod = TempData["DeliveryMethod"] as string ?? string.Empty;
            ViewBag.PaymentMethod = TempData["PaymentMethod"] as string ?? string.Empty;
            ViewBag.DeliveryDetails = TempData["DeliveryDetails"] as string ?? string.Empty;
            ViewBag.PaymentReference = TempData["PaymentReference"] as string ?? string.Empty;
            ViewBag.RenterNote = TempData["RenterNote"] as string ?? string.Empty;
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBooking(
            int itemId,
            DateTime startDate,
            DateTime endDate,
            string? deliveryMethod,
            string? deliveryDetails,
            string? paymentMethod,
            string? paymentReference,
            string? renterNote)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Challenge();
            var currentUser = await _context.Users.FindAsync(userId);
            if (currentUser == null) return Challenge();

            if (!currentUser.IsVerified)
            {
                TempData["Error"] = "يجب توثيق هويتك أولاً قبل إرسال طلب حجز.";
                return RedirectToAction("Verify", "Verification");
            }

            var item = await _context.Items.FindAsync(itemId);
            if (item == null) return NotFound();

            if (endDate.Date < startDate.Date)
            {
                TempData["Error"] = "يجب أن يكون تاريخ الإرجاع في تاريخ الاستلام أو بعده.";
                return BackToCreate(itemId, startDate, endDate, deliveryMethod, deliveryDetails, paymentMethod, paymentReference, renterNote);
            }

            if (!BusinessRules.IsWithinAdvanceWindow(startDate, DateTime.Today))
            {
                TempData["Error"] = $"يجب أن يكون تاريخ الاستلام اليوم أو خلال {BusinessRules.MaxAdvanceDays} يوماً قادماً.";
                return BackToCreate(itemId, startDate, endDate, deliveryMethod, deliveryDetails, paymentMethod, paymentReference, renterNote);
            }

            var days = BusinessRules.RentalDays(startDate, endDate);
            if (days < BusinessRules.MinBookingDays || days > BusinessRules.MaxBookingDays)
            {
                TempData["Error"] = $"مدة الحجز يجب أن تكون بين {BusinessRules.MinBookingDays} و{BusinessRules.MaxBookingDays} يوماً (يوم الاستلام محسوب ضمن المدة).";
                return BackToCreate(itemId, startDate, endDate, deliveryMethod, deliveryDetails, paymentMethod, paymentReference, renterNote);
            }

            if (item.OwnerId == userId)
            {
                TempData["Error"] = "لا يمكنك حجز غرض تملكه.";
                return RedirectToAction("Details", "Items", new { id = itemId });
            }

            if (item.ApprovalStatus != ListingApprovalStatus.Approved || !item.IsActive)
            {
                TempData["Error"] = "هذا الغرض غير متاح حالياً للحجز.";
                return RedirectToAction("Details", "Items", new { id = itemId });
            }

            if (deliveryMethod == null || !AllowedDeliveryMethods.Contains(deliveryMethod))
            {
                TempData["Error"] = "يرجى اختيار طريقة تسليم واضحة.";
                return BackToCreate(itemId, startDate, endDate, deliveryMethod, deliveryDetails, paymentMethod, paymentReference, renterNote);
            }

            if (paymentMethod == null || !AllowedPaymentMethods.Contains(paymentMethod))
            {
                TempData["Error"] = "يرجى اختيار طريقة دفع متاحة وإدخال بياناتها.";
                return BackToCreate(itemId, startDate, endDate, deliveryMethod, deliveryDetails, paymentMethod, paymentReference, renterNote);
            }

            if (string.IsNullOrWhiteSpace(deliveryDetails))
            {
                TempData["Error"] = deliveryMethod == "Courier"
                    ? "يرجى إدخال العنوان الذي سيُرسل إليه الغرض مع مندوب التوصيل."
                    : "يرجى تحديد مكان المقابلة لاستلام الغرض.";
                return BackToCreate(itemId, startDate, endDate, deliveryMethod, deliveryDetails, paymentMethod, paymentReference, renterNote);
            }

            if (paymentMethod != "CashOnDelivery" && string.IsNullOrWhiteSpace(paymentReference))
            {
                TempData["Error"] = "يرجى إدخال رقم العملية أو بيانات التحويل التي تؤكد الدفع.";
                return BackToCreate(itemId, startDate, endDate, deliveryMethod, deliveryDetails, paymentMethod, paymentReference, renterNote);
            }

            if (renterNote != null && renterNote.Length > 500)
            {
                TempData["Error"] = "ملاحظة المستأجر يجب ألا تتجاوز 500 حرف.";
                return BackToCreate(itemId, startDate, endDate, deliveryMethod, deliveryDetails, paymentMethod, paymentReference, renterNote);
            }

            var candidateBookings = await _context.Bookings
                .Where(b => b.ItemId == itemId
                            && b.Status != BookingStatus.Cancelled
                            && b.Status != BookingStatus.Rejected
                            && b.StartDate <= endDate.Date
                            && b.EndDate >= startDate.Date)
                .ToListAsync();
            var candidateBlocks = await _context.AvailabilityBlocks
                .Where(a => a.ItemId == itemId && a.StartDate <= endDate.Date && a.EndDate >= startDate.Date)
                .ToListAsync();

            var overlappingBooking = candidateBookings.Any(b =>
                BusinessRules.DatesOverlap(startDate, endDate, b.StartDate, b.EndDate));
            var overlappingBlock = candidateBlocks.Any(a =>
                BusinessRules.DatesOverlap(startDate, endDate, a.StartDate, a.EndDate));
            if (overlappingBooking || overlappingBlock)
            {
                TempData["Error"] = "النافذة المختارة تتعارض مع فترة محجوزة. راجع حالة التوفر واختر فترة أخرى.";
                return BackToCreate(itemId, startDate, endDate, deliveryMethod, deliveryDetails, paymentMethod, paymentReference, renterNote);
            }

            var booking = new Booking
            {
                ItemId = itemId,
                RenterId = userId,
                StartDate = startDate.Date,
                EndDate = endDate.Date,
                TotalRentalAmount = BusinessRules.RentalTotal(days, item.PricePerDay),
                EscrowDepositAmount = item.SecurityDeposit,
                Status = BookingStatus.Pending,
                DeliveryMethod = deliveryMethod,
                DeliveryDetails = deliveryDetails.Trim(),
                PaymentMethod = paymentMethod,
                PaymentReference = paymentMethod == "CashOnDelivery" ? null : paymentReference?.Trim(),
                RenterNote = string.IsNullOrWhiteSpace(renterNote) ? null : renterNote.Trim()
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم إنشاء طلب الحجز. سيُراجع المالك الطلب، ثم تظهر حالة التسليم والفحص هنا.";
            return RedirectToAction("MyBookings");
        }

        public async Task<IActionResult> MyBookings()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var bookings = await _context.Bookings
                .Include(b => b.Item)
                    .ThenInclude(i => i.Owner)
                .Include(b => b.Inspections)
                .Include(b => b.DisputeLog)
                .Where(b => b.RenterId == userId)
                .OrderByDescending(b => b.Id)
                .ToListAsync();

            return View(bookings);
        }

        public async Task<IActionResult> OwnerRequests()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var requests = await _context.Bookings
                .Include(b => b.Item)
                .Include(b => b.Renter)
                .Include(b => b.Inspections)
                .Include(b => b.DisputeLog)
                .Where(b => b.Item.OwnerId == userId)
                .OrderByDescending(b => b.Id)
                .ToListAsync();

            return View(requests);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = await _context.Bookings
                .Include(b => b.Item)
                    .ThenInclude(i => i.Owner)
                .Include(b => b.Renter)
                .Include(b => b.Inspections)
                .Include(b => b.DisputeLog)
                .Include(b => b.EscrowTransactions)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (booking == null) return NotFound();

            var isOwner = booking.Item?.OwnerId == userId;
            if (booking.RenterId != userId && !isOwner) return Forbid();

            // Phone numbers stay private until the request is approved and actually moving forward.
            ViewBag.CanShowPhone = booking.Status is BookingStatus.Approved
                or BookingStatus.Active
                or BookingStatus.Completed
                or BookingStatus.Disputed;
            ViewBag.IsOwner = isOwner;
            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = await _context.Bookings
                .Include(b => b.Item)
                .FirstOrDefaultAsync(b => b.Id == id && b.Item.OwnerId == userId);
            if (booking == null) return NotFound();

            if (booking.Status != BookingStatus.Pending)
            {
                TempData["Error"] = "تمت مراجعة هذا الطلب بالفعل.";
                return RedirectToAction("OwnerRequests");
            }

            booking.Status = BookingStatus.Approved;
            booking.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _escrow.HoldAsync(booking.Id, userId);

            // A confirmed window takes priority: all other pending requests that
            // overlap it are rejected automatically and their renters are told why.
            var competing = await _context.Bookings
                .Where(b => b.Id != booking.Id && b.ItemId == booking.ItemId && b.Status == BookingStatus.Pending)
                .ToListAsync();
            var rejected = competing
                .Where(b => BusinessRules.DatesOverlap(booking.StartDate, booking.EndDate, b.StartDate, b.EndDate))
                .ToList();
            foreach (var other in rejected)
            {
                other.Status = BookingStatus.Rejected;
                other.UpdatedAt = DateTime.UtcNow;
            }
            if (rejected.Count > 0) await _context.SaveChangesAsync();

            foreach (var other in rejected)
            {
                await _notifications.NotifyAsync(other.RenterId,
                    $"تم رفض طلب الحجز رقم {other.Id} تلقائياً لتعارضه مع حجز مقبول على الغرض رقم {booking.ItemId}.",
                    "booking", "/Bookings/MyBookings");
            }

            TempData["Success"] = rejected.Count > 0
                ? $"تم قبول الطلب وحجز التأمين، ورُفض {rejected.Count} طلب متعارض تلقائياً."
                : "تم قبول الطلب. يمكن الآن تسليم الغرض وتوثيق حالة الاستلام.";
            return RedirectToAction("OwnerRequests");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = await _context.Bookings
                .Include(b => b.Item)
                .FirstOrDefaultAsync(b => b.Id == id && b.Item.OwnerId == userId);
            if (booking == null) return NotFound();

            if (booking.Status != BookingStatus.Pending)
            {
                TempData["Error"] = "تمت مراجعة هذا الطلب بالفعل.";
                return RedirectToAction("OwnerRequests");
            }

            booking.Status = BookingStatus.Rejected;
            booking.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _notifications.NotifyAsync(booking.RenterId,
                $"رفض مالك الغرض طلب الحجز رقم {booking.Id}.", "booking", "/Bookings/MyBookings");
            TempData["Success"] = "تم رفض طلب الاستعارة.";
            return RedirectToAction("OwnerRequests");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? reason)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = await _context.Bookings
                .Include(b => b.Item)
                .FirstOrDefaultAsync(b => b.Id == id && (b.RenterId == userId || b.Item.OwnerId == userId));
            if (booking == null) return NotFound();

            var now = DateTime.Now;
            if (!BusinessRules.CanCancel(booking, now))
            {
                TempData["Error"] = "لا يمكن إلغاء الحجز إلا إذا كان قيد الانتظار أو موافقاً عليه ولم يُسلَّم، وقبل تاريخ الاستلام.";
                return RedirectToAction(booking.Item?.OwnerId == userId ? "OwnerRequests" : "MyBookings");
            }

            var byRenter = booking.RenterId == userId;
            if (byRenter && (booking.StartDate - now).TotalHours < 48)
            {
                booking.LateCancellationByRenter = true;
                var renter = await _context.Users.FindAsync(userId);
                if (renter != null) renter.LateCancellationCount++;
            }

            if (!string.IsNullOrWhiteSpace(reason))
            {
                var trimmed = reason.Trim();
                booking.CancellationReason = trimmed.Length > 300 ? trimmed[..300] : trimmed;
            }

            booking.Status = BookingStatus.Cancelled;
            booking.UpdatedAt = DateTime.UtcNow;

            if (booking.DepositStatus == DepositStatus.Held)
                await _escrow.ReleaseAsync(booking.Id, userId, "إلغاء الحجز قبل التسليم");

            await _context.SaveChangesAsync();

            var otherPartyId = byRenter ? booking.Item!.OwnerId : booking.RenterId;
            var lateNote = booking.LateCancellationByRenter ? " (إلغاء متأخر من المستأجر)" : string.Empty;
            await _notifications.NotifyAsync(otherPartyId,
                $"تم إلغاء الحجز رقم {booking.Id}{lateNote} وإطلاق أي تأمين محتجز.",
                "booking", "/Bookings/Details/" + booking.Id);

            TempData["Success"] = booking.LateCancellationByRenter
                ? "تم إلغاء الحجز. سُجّل إلغاء متأخر خلال أقل من 48 ساعة قبل الاستلام."
                : "تم إلغاء الحجز وإطلاق أي تأمين محتجز.";
            return RedirectToAction(byRenter ? "MyBookings" : "OwnerRequests");
        }

        private IActionResult BackToCreate(
            int itemId,
            DateTime startDate,
            DateTime endDate,
            string? deliveryMethod,
            string? deliveryDetails,
            string? paymentMethod,
            string? paymentReference,
            string? renterNote)
        {
            TempData["DeliveryMethod"] = deliveryMethod ?? string.Empty;
            TempData["DeliveryDetails"] = deliveryDetails ?? string.Empty;
            TempData["PaymentMethod"] = paymentMethod ?? string.Empty;
            TempData["PaymentReference"] = paymentReference ?? string.Empty;
            TempData["RenterNote"] = renterNote ?? string.Empty;
            return RedirectToAction("Create", new { itemId, startDate, endDate });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkDelivered(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = await _context.Bookings
                .Include(b => b.Item)
                .FirstOrDefaultAsync(b => b.Id == id && (b.RenterId == userId || b.Item.OwnerId == userId));
            if (booking == null) return NotFound();

            if (booking.Status is BookingStatus.Rejected or BookingStatus.Cancelled or BookingStatus.Disputed)
            {
                TempData["Error"] = "لا يمكن تسليم غرض في طلب مرفوض أو ملغي أو متنازع عليه.";
                return RedirectToAction("MyBookings");
            }

            if (booking.IsDelivered)
            {
                TempData["Success"] = "الغرض مُسجّل كمُسلّم بالفعل.";
                return RedirectToAction("MyBookings");
            }

            booking.IsDelivered = true;
            booking.DeliveredAt = DateTime.Now;
            if (booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.Approved)
                booking.Status = BookingStatus.Active;

            await _context.SaveChangesAsync();
            TempData["Success"] = "تم تسجيل استلام الغرض. صوّر الـ 6 زوايا قبل إعادته للمالك.";
            return RedirectToAction("MyBookings");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkReturned(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id && b.RenterId == userId);
            if (booking == null) return NotFound();

            if (!booking.IsDelivered)
            {
                TempData["Error"] = "سجّل استلام الغرض أولاً قبل تسجيل إعادته.";
                return RedirectToAction("MyBookings");
            }

            var hasReturnInspection = await _context.BookingInspections
                .AnyAsync(i => i.BookingId == id && i.Stage == InspectionStage.ReturnByRenter);
            if (!hasReturnInspection)
            {
                TempData["Error"] = "صوّر الغرض من الزوايا الست وسجّل فحص الإرجاع قبل تسجيل إعادته للمالك.";
                return RedirectToAction("InspectionStudio", "Disputes", new { bookingId = id, mode = "return" });
            }

            if (booking.ReturnedAt != null)
            {
                TempData["Success"] = "تم تسجيل إعادة الغرض بالفعل، بانتظار فحص المالك.";
                return RedirectToAction("MyBookings");
            }

            booking.ReturnedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            TempData["Success"] = "تم تسجيل إعادة الغرض. سيُطلب من المالك تصوير الـ 6 زوايا عند الاستلام.";
            return RedirectToAction("MyBookings");
        }
    }
}
