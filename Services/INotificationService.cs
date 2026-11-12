namespace SalefnyMVC.Services;

/// <summary>Safe read model. Never expose the Notification entity (it links to ApplicationUser, which holds PasswordHash).</summary>
public sealed record NotificationDto(int Id, string Message, bool IsRead, DateTime CreatedAt);

public interface INotificationService
{
    Task<IReadOnlyList<NotificationDto>> GetLatestAsync(string userId, int take = 20, bool unreadOnly = false, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default);
    Task<ServiceResult> MarkAsReadAsync(int notificationId, string userId, CancellationToken ct = default);
    Task<int> MarkAllAsReadAsync(string userId, CancellationToken ct = default);
}
