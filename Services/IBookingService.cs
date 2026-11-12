using SalefnyMVC.Models.Entities;
using SalefnyMVC.ViewModels;

namespace SalefnyMVC.Services;

public sealed record BookingPriceBreakdown(int Days, decimal RentalAmount, decimal DepositAmount, decimal PlatformFee)
{
    public decimal GrandTotal => RentalAmount + DepositAmount + PlatformFee;
}


public interface IBookingService
{
    Task<BookingPriceBreakdown?> CalculatePriceAsync(int itemId, DateTime start, DateTime end, CancellationToken ct = default);
    Task<bool> IsAvailableAsync(int itemId, DateTime start, DateTime end, int? excludeBookingId = null, CancellationToken ct = default);

    Task<ServiceResult<Booking>> CreateAsync(BookingRequestViewModel model, string renterId, CancellationToken ct = default);
    Task<ServiceResult<Booking>> ApproveAsync(int bookingId, string ownerId, CancellationToken ct = default);
    Task<ServiceResult<Booking>> RejectAsync(int bookingId, string ownerId, CancellationToken ct = default);
    Task<ServiceResult<Booking>> CancelAsync(int bookingId, string userId, CancellationToken ct = default);
    Task<ServiceResult<Booking>> MarkActiveAsync(int bookingId, string ownerId, CancellationToken ct = default);  // handover
    Task<ServiceResult<Booking>> CompleteAsync(int bookingId, string ownerId, CancellationToken ct = default);

    Task<IReadOnlyList<Booking>> GetRenterBookingsAsync(string renterId, CancellationToken ct = default);
    Task<IReadOnlyList<Booking>> GetOwnerRequestsAsync(string ownerId, CancellationToken ct = default);
}
