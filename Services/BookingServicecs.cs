using Microsoft.EntityFrameworkCore;
using SalefnyMVC.Data;
using SalefnyMVC.Models.Entities;
using SalefnyMVC.Models.Enums;

namespace SalefnyMVC.Services
{
    public class BookingService : IBookingService
    {
        private readonly SalefnyDbContext _context;

        public BookingService(SalefnyDbContext context)
        {
            _context = context;
        }

        public async Task<bool> IsItemAvailableAsync(int itemId, DateTime requestedStart, DateTime requestedEnd)
        {
            // 3NF Double-Booking Prevention: Checks for date range overlaps
            bool hasConflict = await _context.Bookings
                .Where(b => b.ItemId == itemId && b.Status == BookingStatus.Approved)
                .AnyAsync(b => requestedStart < b.EndDate && b.StartDate < requestedEnd);

            return !hasConflict;
        }

        public decimal CalculateRentalTotal(decimal pricePerDay, int days, decimal securityDeposit)
        {
            if (days <= 0) days = 1;
            return (pricePerDay * days) + securityDeposit;
        }

        public async Task<Booking> CreateBookingAsync(int itemId, string renterId, DateTime startDate, DateTime endDate)
        {
            var item = await _context.Items.FindAsync(itemId);
            if (item == null) throw new Exception("المنتج غير موجود.");

            int days = (endDate - startDate).Days;
            if (days <= 0) days = 1;

            var booking = new Booking
            {
                ItemId = itemId,
                RenterId = renterId,
                StartDate = startDate,
                EndDate = endDate,
                TotalRentalAmount = item.PricePerDay * days,
                EscrowDepositAmount = item.SecurityDeposit,
                Status = BookingStatus.Pending
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            return booking;
        }
    }
}