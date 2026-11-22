using SalefnyMVC.Models.Entities;
using SalefnyMVC.Models.Enums;

namespace SalefnyMVC.Services
{
    /// <summary>
    /// Pure business rules shared by controllers and unit tests.
    /// Keeping these rules independent of DbContext makes them easy to test.
    /// </summary>
    public static class BusinessRules
    {
        public const int MinBookingDays = 1;
        public const int MaxBookingDays = 30;
        public const int MaxAdvanceDays = 180;
        public const int MaxItemPhotos = 8;

        public static int RentalDays(DateTime startDate, DateTime endDate)
        {
            var days = (endDate.Date - startDate.Date).Days + 1;
            return days < 0 ? 0 : days;
        }

        public static bool DatesOverlap(DateTime newStart, DateTime newEnd, DateTime existingStart, DateTime existingEnd)
            => newStart.Date <= existingEnd.Date && newEnd.Date >= existingStart.Date;

        public static bool IsWithinAdvanceWindow(DateTime startDate, DateTime today)
            => startDate.Date >= today.Date && startDate.Date <= today.Date.AddDays(MaxAdvanceDays);

        public static decimal RentalTotal(int days, decimal pricePerDay)
            => days <= 0 ? 0 : days * pricePerDay;

        public static decimal AverageRating(IEnumerable<int> ratings)
        {
            var values = ratings.ToList();
            return values.Count == 0 ? 0 : Math.Round((decimal)values.Average(), 2);
        }

        public static bool CanReview(Booking booking, string reviewerId)
            => booking.Status == BookingStatus.Completed
               && (reviewerId == booking.RenterId || reviewerId == booking.Item?.OwnerId);

        public static bool CanCancel(Booking booking, DateTime now)
            => booking.Status is BookingStatus.Pending or BookingStatus.Approved
               && !booking.IsDelivered
               && booking.StartDate.Date > now.Date;
    }
}
