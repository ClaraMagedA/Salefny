using Microsoft.AspNetCore.Authorization;
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
    [Authorize]
    public class DisputesController : Controller
    {
        private readonly SalefnyDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly INotificationService _notifications;
        private readonly IEscrowService _escrow;

        public DisputesController(
            SalefnyDbContext context,
            IWebHostEnvironment environment,
            INotificationService notifications,
            IEscrowService escrow)
        {
            _context = context;
            _environment = environment;
            _notifications = notifications;
            _escrow = escrow;
        }

        [HttpGet]
        public async Task<IActionResult> InspectionStudio(int bookingId, string? mode = "return")
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = await _context.Bookings
                .Include(b => b.Item)
                    .ThenInclude(i => i.ItemImages)
                .Include(b => b.Renter)
                .Include(b => b.Inspections)
                .FirstOrDefaultAsync(b => b.Id == bookingId);
            if (booking == null) return NotFound();

            var isRenter = booking.RenterId == userId;
            var isOwner = booking.Item.OwnerId == userId;
            if (!isRenter && !isOwner) return Forbid();

            var stage = isRenter ? InspectionStage.ReturnByRenter : InspectionStage.ReceiptByOwner;
            var model = new InspectionStudioViewModel
            {
                Booking = booking,
                Stage = stage,
                IsRenter = isRenter,
                DamageMode = string.Equals(mode, "damage", StringComparison.OrdinalIgnoreCase),
                ExistingInspection = booking.Inspections.FirstOrDefault(i => i.Stage == stage)
            };

            if (stage == InspectionStage.ReturnByRenter && !booking.IsDelivered)
                TempData["Error"] = "يمكن توثيق فحص الإرجاع بعد تسجيل استلام الغرض أولاً.";
            else if (stage == InspectionStage.ReceiptByOwner && booking.ReturnedAt == null)
                TempData["Error"] = "سيُطلب منك فحص الاستلام بعد أن يسجّل المستأجر إعادة الغرض.";

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitInspection(
            int bookingId,
            InspectionStage stage,
            List<IFormFile>? inspectionPhotos,
            bool damageReported,
            string? damageDescription)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = await _context.Bookings
                .Include(b => b.Item)
                    .ThenInclude(i => i.ItemImages)
                .Include(b => b.Renter)
                .Include(b => b.Inspections)
                .FirstOrDefaultAsync(b => b.Id == bookingId);
            if (booking == null) return NotFound();

            var isRenter = booking.RenterId == userId;
            var isOwner = booking.Item.OwnerId == userId;
            if (!isRenter && !isOwner) return Forbid();

            var validStage = (isRenter && stage == InspectionStage.ReturnByRenter)
                             || (isOwner && stage == InspectionStage.ReceiptByOwner);
            if (!validStage) return Forbid();

            if (stage == InspectionStage.ReturnByRenter && !booking.IsDelivered)
            {
                TempData["Error"] = "سجّل استلام الغرض أولاً قبل رفع صور الإرجاع.";
                return RedirectToAction("MyBookings", "Bookings");
            }

            if (stage == InspectionStage.ReceiptByOwner && booking.ReturnedAt == null)
            {
                TempData["Error"] = "لم يسجّل المستأجر إعادة الغرض بعد، لذا لا يمكن فحص الاستلام.";
                return RedirectToAction("OwnerRequests", "Bookings");
            }

            if (inspectionPhotos == null || inspectionPhotos.Count < 6 || inspectionPhotos.Any(f => f == null || f.Length == 0))
            {
                TempData["Error"] = "يرجى إرفاق صور للزوايا الست جميعها قبل الاعتماد.";
                return RedirectToAction("InspectionStudio", new { bookingId, mode = damageReported ? "damage" : "return" });
            }

            if (damageReported && string.IsNullOrWhiteSpace(damageDescription))
            {
                TempData["Error"] = "يرجى وصف الضرر الذي لاحظته قبل إرسال التقرير.";
                return RedirectToAction("InspectionStudio", new { bookingId, mode = "damage" });
            }

            var folderRelative = $"/uploads/inspections/{bookingId}/{(stage == InspectionStage.ReturnByRenter ? "return" : "receipt")}";
            var folderFull = Path.Combine(_environment.WebRootPath,
                folderRelative.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            // Replace any earlier inspection for this stage so only the latest photos count.
            var existingInspection = booking.Inspections.FirstOrDefault(i => i.Stage == stage);
            if (existingInspection != null && !string.IsNullOrWhiteSpace(existingInspection.PhotoPaths))
            {
                foreach (var oldPath in existingInspection.PhotoPaths.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    var oldFull = Path.Combine(_environment.WebRootPath,
                        oldPath.Trim().TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(oldFull)) System.IO.File.Delete(oldFull);
                }
            }

            Directory.CreateDirectory(folderFull);
            var savedPaths = new List<string>();
            for (var i = 0; i < 6; i++)
            {
                var file = inspectionPhotos[i];
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp")) extension = ".jpg";

                var fileName = $"angle-{i + 1}{extension}";
                var relativePath = $"{folderRelative}/{fileName}";
                var fullPath = Path.Combine(folderFull, fileName);
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                savedPaths.Add(relativePath);
            }

            var photoPaths = string.Join(',', savedPaths);
            if (existingInspection == null)
            {
                existingInspection = new BookingInspection
                {
                    BookingId = bookingId,
                    Stage = stage
                };
                _context.BookingInspections.Add(existingInspection);
            }

            existingInspection.RecordedBy = userId;
            existingInspection.PhotoPaths = photoPaths;
            existingInspection.DamageReported = damageReported;
            existingInspection.DamageDescription = damageReported ? damageDescription!.Trim() : null;
            existingInspection.CreatedAt = DateTime.Now;

            if (damageReported)
            {
                var dispute = await _context.DisputeLogs.FirstOrDefaultAsync(d => d.BookingId == bookingId);
                if (dispute == null)
                {
                    dispute = new DisputeLog { BookingId = bookingId };
                    _context.DisputeLogs.Add(dispute);
                }
                dispute.ClaimDescription = damageDescription!.Trim();
                dispute.ReturnAnglePhotoPaths = photoPaths;
                dispute.LoggedAt = DateTime.Now;
                dispute.IsResolved = false;
            }

            if (damageReported)
            {
                // Any confirmed damage opens a dispute; the deposit is frozen until an admin rules on it.
                booking.Status = BookingStatus.Disputed;
                booking.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                await _escrow.FreezeAsync(bookingId, userId, $"ضرر مُبلَّغ عنه في فحص {stage} للحجز رقم {bookingId}");

                var detailsUrl = "/Bookings/Details/" + bookingId;
                await _notifications.NotifyAsync(booking.RenterId,
                    $"تم فتح بلاغ ضرر للحجز رقم {bookingId} وتجميد التأمين لحين مراجعة الإدارة.",
                    "dispute", detailsUrl);
                await _notifications.NotifyAsync(booking.Item.OwnerId,
                    $"تم تسجيل بلاغ الضرر للحجز رقم {bookingId} وتجميد الوديعة. سيُراجع من الإدارة.",
                    "dispute", detailsUrl);
                await _notifications.NotifyAdminsAsync(
                    $"بلاغ ضرر جديد للحجز رقم {bookingId} من {(isRenter ? "المستأجر" : "المالك")}. يلزم المراجعة.",
                    "dispute", "/Admin/Disputes");
            }
            else if (stage == InspectionStage.ReceiptByOwner)
            {
                // Owner receipt inspection with no damage closes the rental and releases the escrow.
                booking.Status = BookingStatus.Completed;
                booking.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                await _escrow.ReleaseAsync(bookingId, userId, "اكتمل فحص الاستلام دون أضرار");
            }
            else
            {
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = stage == InspectionStage.ReturnByRenter
                ? damageReported
                    ? "تم حفظ فحص الإرجاع سداسي الزوايا وفتح بلاغ ضرر. الوديعة مجمدة لحين بت النزاع."
                    : "تم حفظ فحص الإرجاع سداسي الزوايا. سجّل إعادة الغرض من «تأجيراتي» ليظهر الفحص للمالك."
                : damageReported
                    ? "تم تسجيل ضرر عند الاستلام وفتح بلاغ نزاع. الوديعة مجمدة لحين مراجعة الإدارة."
                    : "اكتمل فحص الاستلام دون أضرار، وأُغلقت عملية التأجير وأُطلق مبلغ التأمين.";

            return RedirectToAction(isRenter ? "MyBookings" : "OwnerRequests", "Bookings");
        }
    }
}
