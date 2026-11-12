using Microsoft.EntityFrameworkCore;
using SalefnyMVC.Data;

namespace SalefnyMVC.Services;

public class NotificationService : INotificationService
{
    private const int MaxPageSize = 100;
    private const int MaxBulkUpdate = 1000;
    private const string NotFound = "الإشعار غير موجود.";

    private readonly SalefnyDbContext _db;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(SalefnyDbContext db, ILogger<NotificationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<NotificationDto>> GetLatestAsync(
        string userId, int take = 20, bool unreadOnly = false, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return Array.Empty<NotificationDto>();

        take = Math.Clamp(take, 1, MaxPageSize);   // callers can never ask for an unbounded list

        var query = _db.Notifications.AsNoTracking().Where(n => n.UserId == userId);
        if (unreadOnly) query = query.Where(n => !n.IsRead);

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .Take(take)
            .Select(n => new NotificationDto(n.Id, n.Message, n.IsRead, n.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return 0;

        return await _db.Notifications.AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead, ct);
    }

    public async Task<ServiceResult> MarkAsReadAsync(int notificationId, string userId, CancellationToken ct = default)
    {
        if (notificationId <= 0 || string.IsNullOrWhiteSpace(userId))
            return ServiceResult.Fail(NotFound);

        // The UserId filter is the ownership check. "Not found" and "not yours" give the same answer (no IDOR).
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, ct);

        if (notification is null)
        {
            _logger.LogWarning("User {UserId} tried to mark notification {Id} (missing or not theirs)", userId, notificationId);
            return ServiceResult.Fail(NotFound);
        }

        if (notification.IsRead) return ServiceResult.Ok();

        notification.IsRead = true;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<int> MarkAllAsReadAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return 0;

        var unread = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .Take(MaxBulkUpdate)
            .ToListAsync(ct);

        foreach (var n in unread) n.IsRead = true;

        if (unread.Count > 0) await _db.SaveChangesAsync(ct);
        return unread.Count;
    }
}