using SalefnyMVC.Models.Entities;

namespace SalefnyMVC.Services
{
    public interface IBookingService
    {
        Task<bool> IsItemAvailableAsync(int itemId, DateTime requestedStart, DateTime requestedEnd);
        decimal CalculateRentalTotal(decimal pricePerDay, int days, decimal securityDeposit);
        Task<Booking> CreateBookingAsync(int itemId, string renterId, DateTime startDate, DateTime endDate);
    }
}