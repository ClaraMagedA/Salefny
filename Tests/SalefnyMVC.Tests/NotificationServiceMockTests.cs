using Moq;
using SalefnyMVC.Services;
using Xunit;

namespace SalefnyMVC.Tests;

/// <summary>
/// A tiny test-only collaborator that depends only on INotificationService.
/// It deliberately never touches DbContext, SMTP, SMS, or the web host.
/// </summary>
internal sealed class BookingNotificationHelper
{
    private readonly INotificationService _notifications;

    public BookingNotificationHelper(INotificationService notifications)
        => _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));

    public Task NotifyBookingApprovedAsync(string renterId, int bookingId)
        => _notifications.NotifyAsync(
            renterId,
            $"Booking {bookingId} was approved.",
            "booking-approved",
            $"/Bookings/Details/{bookingId}");

    public Task NotifyWatchersAsync(IEnumerable<string> userIds, string message)
        => _notifications.NotifyManyAsync(userIds, message, "booking-update");

    public Task MarkAllReadAsync(string userId)
        => _notifications.MarkAllReadAsync(userId);
}

public class NotificationServiceMockTests
{
    [Fact]
    public async Task NotifyBookingApprovedAsync_UsesInjectedMockWithExpectedArguments()
    {
        var mock = new Mock<INotificationService>(MockBehavior.Strict);
        mock.Setup(s => s.NotifyAsync(
                "renter-1",
                "Booking 42 was approved.",
                "booking-approved",
                "/Bookings/Details/42"))
            .Returns(Task.CompletedTask)
            .Verifiable();

        await new BookingNotificationHelper(mock.Object).NotifyBookingApprovedAsync("renter-1", 42);

        mock.Verify(s => s.NotifyAsync(
            "renter-1",
            "Booking 42 was approved.",
            "booking-approved",
            "/Bookings/Details/42"), Times.Once);
        mock.Verify();
        mock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task NotifyWatchersAsync_ForwardsRecipientsAndMessageToMock()
    {
        var mock = new Mock<INotificationService>();
        mock.Setup(s => s.NotifyManyAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        await new BookingNotificationHelper(mock.Object)
            .NotifyWatchersAsync(new[] { "owner-1", "admin-1" }, "Booking 7 changed.");

        mock.Verify(s => s.NotifyManyAsync(
            It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "owner-1", "admin-1" })),
            "Booking 7 changed.",
            "booking-update",
            null), Times.Once);
    }

    [Fact]
    public async Task MarkAllReadAsync_DelegatesOnceToMock()
    {
        var mock = new Mock<INotificationService>();
        mock.Setup(s => s.MarkAllReadAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        await new BookingNotificationHelper(mock.Object).MarkAllReadAsync("renter-1");

        mock.Verify(s => s.MarkAllReadAsync("renter-1"), Times.Once);
    }

    [Fact]
    public async Task Helper_PropagatesExceptionThrownByMock()
    {
        var mock = new Mock<INotificationService>();
        mock.Setup(s => s.NotifyAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ThrowsAsync(new InvalidOperationException("notification service unavailable"));

        var helper = new BookingNotificationHelper(mock.Object);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => helper.NotifyBookingApprovedAsync("renter-1", 9));
        Assert.Equal("notification service unavailable", error.Message);
    }
}
