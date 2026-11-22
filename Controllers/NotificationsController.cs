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
    public class NotificationsController : Controller
    {
        private readonly SalefnyDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;

        public NotificationsController(
            SalefnyDbContext context,
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ThenByDescending(n => n.Id)
                .ToListAsync();

            var unreadCount = notifications.Count(n => !n.IsRead);
            ViewBag.UnreadCount = unreadCount;
            ViewBag.NotificationCount = unreadCount;
            return View(notifications);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            // Ownership is verified before any write, so ids belonging to other users are ignored.
            var owned = await _context.Notifications.AnyAsync(n => n.Id == id && n.UserId == userId);
            if (!owned)
            {
                TempData["Error"] = "لم يتم العثور على هذا الإشعار.";
                return RedirectToAction(nameof(Index));
            }

            await _notificationService.MarkReadAsync(id, userId);
            TempData["Success"] = "تم تحديد الإشعار كمقروء.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAll()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            await _notificationService.MarkAllReadAsync(userId);
            TempData["Success"] = "تم تحديد جميع الإشعارات كمقروءة.";
            return RedirectToAction(nameof(Index));
        }
    }
}
