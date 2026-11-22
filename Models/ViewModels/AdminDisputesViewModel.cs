using SalefnyMVC.Models.Enums;

namespace SalefnyMVC.ViewModels
{
    public class AdminDisputesViewModel
    {
        public List<AdminDisputeRow> Disputes { get; set; } = new();
        public bool ShowResolved { get; set; }
        public int PendingCount { get; set; }
    }

    public class AdminDisputeRow
    {
        public int Id { get; set; }
        public string ClaimDescription { get; set; } = string.Empty;
        public bool IsResolved { get; set; }
        public decimal DeductedDepositAmount { get; set; }
        public string? ResolutionNote { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime LoggedAt { get; set; }
        public List<string> ReturnPhotos { get; set; } = new();

        public int BookingId { get; set; }
        public BookingStatus BookingStatus { get; set; }
        public DepositStatus DepositStatus { get; set; }
        public decimal EscrowDepositAmount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public int ItemId { get; set; }
        public string ItemTitle { get; set; } = string.Empty;
        public string? ItemImagePath { get; set; }

        public string RenterId { get; set; } = string.Empty;
        public string RenterName { get; set; } = string.Empty;
        public string RenterEmail { get; set; } = string.Empty;
        public string OwnerId { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string OwnerEmail { get; set; } = string.Empty;
    }
}
