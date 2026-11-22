using Microsoft.EntityFrameworkCore;
using SalefnyMVC.Data;
using SalefnyMVC.Models.Entities;

namespace SalefnyMVC.Services
{
    public interface INotificationService
    {
        Task NotifyAsync(string userId, string message, string eventType, string? actionUrl = null);
        Task NotifyAdminsAsync(string message, string eventType, string? actionUrl = null);
        Task NotifyManyAsync(IEnumerable<string> userIds, string message, string eventType, string? actionUrl = null);
        Task MarkReadAsync(int id, string userId);
        Task MarkAllReadAsync(string userId);
    }

    public class NotificationService : INotificationService
    {
        private readonly SalefnyDbContext _context;
        private readonly IEmailSender _emailSender;

        public NotificationService(SalefnyDbContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        public async Task NotifyAsync(string userId, string message, string eventType, string? actionUrl = null)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return;

            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                Message = message,
                EventType = eventType,
                ActionUrl = actionUrl,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            });
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                await _emailSender.SendEmailAsync(user.Email, $"سَلِّفني — {eventType}", $"<p dir='rtl'>{message}</p>");
            }
        }

        public async Task NotifyAdminsAsync(string message, string eventType, string? actionUrl = null)
        {
            var adminIds = await (from ur in _context.UserRoles
                                  join r in _context.Roles on ur.RoleId equals r.Id
                                  where r.Name == "Admin" || r.Name == "SuperAdmin"
                                  select ur.UserId).Distinct().ToListAsync();
            await NotifyManyAsync(adminIds, message, eventType, actionUrl);
        }

        public async Task NotifyManyAsync(IEnumerable<string> userIds, string message, string eventType, string? actionUrl = null)
        {
            foreach (var id in userIds.Distinct())
                await NotifyAsync(id, message, eventType, actionUrl);
        }

        public async Task MarkReadAsync(int id, string userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
            if (notification == null) return;
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }

        public async Task MarkAllReadAsync(string userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();
            foreach (var notification in notifications) notification.IsRead = true;
            await _context.SaveChangesAsync();
        }
    }
}
