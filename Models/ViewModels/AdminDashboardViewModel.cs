using SalefnyMVC.Models.Enums;

namespace SalefnyMVC.ViewModels
{
    /// <summary>
    /// All numbers shown on the admin overview page are computed from the database.
    /// </summary>
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int VerifiedUsers { get; set; }
        public int Owners { get; set; }
        public int ActiveListings { get; set; }
        public int PendingListings { get; set; }
        public int PendingOwnerRequests { get; set; }
        public int PendingVerificationsCount { get; set; }
        public int ActiveDisputesCount { get; set; }

        public Dictionary<BookingStatus, int> BookingsByStatus { get; set; } = new();
        public int ThisMonthBookings { get; set; }
        public decimal RevenueEstimate { get; set; }
        public decimal EscrowHeld { get; set; }
        public decimal EscrowFrozen { get; set; }

        public List<CategoryBookingStat> TopCategories { get; set; } = new();
        public List<MonthlyBookingStat> BookingsPerMonth { get; set; } = new();

        public int GetBookingCount(BookingStatus status)
            => BookingsByStatus.TryGetValue(status, out var count) ? count : 0;

        public int MaxMonthlyBookings
            => BookingsPerMonth.Count == 0 ? 1 : Math.Max(1, BookingsPerMonth.Max(m => m.Count));

        public int MaxCategoryBookings
            => TopCategories.Count == 0 ? 1 : Math.Max(1, TopCategories.Max(c => c.BookingCount));
    }

    public class CategoryBookingStat
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int BookingCount { get; set; }
    }

    public class MonthlyBookingStat
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
