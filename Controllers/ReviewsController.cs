using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalefnyMVC.Data;
using SalefnyMVC.Models.Entities;
using SalefnyMVC.Services;

namespace SalefnyMVC.Controllers
{
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly SalefnyDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRatingService _ratingService;
        private readonly INotificationService _notificationService;

        public ReviewsController(
            SalefnyDbContext context,
            UserManager<ApplicationUser> userManager,
            IRatingService ratingService,
            INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _ratingService = ratingService;
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> Create(int bookingId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var booking = await _context.Bookings
                .Include(b => b.Item)
                .Include(b => b.Renter)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                TempData["Error"] = "لم يتم العثور على هذا الحجز.";
                return RedirectToAction("MyBookings", "Bookings");
            }

            if (booking.Item == null || !BusinessRules.CanReview(booking, userId))
            {
                TempData["Error"] = "يمكن تقييم الحجز بعد اكتماله فقط، ولطرفي الحجز (المستأجر أو المالك) فقط.";
                return RedirectToAction("MyBookings", "Bookings");
            }

            if (await _context.Reviews.AnyAsync(r => r.BookingId == bookingId && r.ReviewerId == userId))
            {
                TempData["Error"] = "لقد قمت بتقييم هذا الحجز من قبل.";
                return RedirectToAction("MyBookings", "Bookings");
            }

            var revieweeId = userId == booking.RenterId ? booking.Item.OwnerId : booking.RenterId;
            var revieweeName = await _context.Users
                .Where(u => u.Id == revieweeId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync();

            ViewBag.BookingId = bookingId;
            ViewBag.ItemTitle = booking.Item.Title;
            ViewBag.RevieweeName = string.IsNullOrWhiteSpace(revieweeName) ? "الطرف الآخر" : revieweeName;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int bookingId, int rating, string? comment)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var booking = await _context.Bookings
                .Include(b => b.Item)
                .Include(b => b.Renter)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                TempData["Error"] = "لم يتم العثور على هذا الحجز.";
                return RedirectToAction("MyBookings", "Bookings");
            }

            if (booking.Item == null || !BusinessRules.CanReview(booking, userId))
            {
                TempData["Error"] = "يمكن تقييم الحجز بعد اكتماله فقط، ولطرفي الحجز فقط.";
                return RedirectToAction("MyBookings", "Bookings");
            }

            if (await _context.Reviews.AnyAsync(r => r.BookingId == bookingId && r.ReviewerId == userId))
            {
                TempData["Error"] = "لقد قمت بتقييم هذا الحجز من قبل.";
                return RedirectToAction("MyBookings", "Bookings");
            }

            if (rating < 1 || rating > 5)
                ModelState.AddModelError(nameof(rating), "يرجى اختيار تقييم من 1 إلى 5 نجوم.");

            if (comment?.Length > 1000)
                ModelState.AddModelError(nameof(comment), "التعليق يجب ألا يتجاوز 1000 حرف.");

            if (!ModelState.IsValid)
            {
                var nameForInvalidInput = await _context.Users
                    .Where(u => u.Id == (userId == booking.RenterId ? booking.Item.OwnerId : booking.RenterId))
                    .Select(u => u.FullName)
                    .FirstOrDefaultAsync();
                ViewBag.BookingId = bookingId;
                ViewBag.ItemTitle = booking.Item.Title;
                ViewBag.RevieweeName = string.IsNullOrWhiteSpace(nameForInvalidInput) ? "الطرف الآخر" : nameForInvalidInput;
                return View();
            }

            var revieweeId = userId == booking.RenterId ? booking.Item.OwnerId : booking.RenterId;

            var review = new Review
            {
                BookingId = bookingId,
                ItemId = booking.ItemId,
                ReviewerId = userId,
                RevieweeId = revieweeId,
                Rating = rating,
                Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            // Keep the reviewee's displayed trust score in sync with the new review.
            await _ratingService.RecalculateAsync(revieweeId);

            var revieweeName = await _context.Users
                .Where(u => u.Id == revieweeId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync();

            var actionUrl = Url.Action("Details", "Items", new { id = booking.ItemId });
            var notificationMessage = $"تقييم جديد من {User.Identity?.Name ?? "مستخدم"} لك على حجز الغرض «{booking.Item.Title}»: {rating} من 5 نجوم.";
            if (notificationMessage.Length > 240)
                notificationMessage = notificationMessage[..240] + "…";

            await _notificationService.NotifyAsync(
                revieweeId,
                notificationMessage,
                "تقييم جديد",
                actionUrl);

            TempData["Success"] = $"شكراً لتقييم {revieweeName ?? "الطرف الآخر"}. تم حفظ تقييمك بنجاح.";
            return RedirectToAction("MyBookings", "Bookings");
        }
    }
}
