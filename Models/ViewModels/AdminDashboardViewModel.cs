using SalefnyMVC.Models.Entities;

namespace SalefnyMVC.ViewModels
{
    public class AdminDashboardViewModel
    {
        public decimal TotalEscrowHoldings { get; set; }
        public int PendingVerificationsCount { get; set; }
        public int ActiveDisputesCount { get; set; }
        public decimal TotalPlatformCommission { get; set; }

        public IEnumerable<IdentityVerification> PendingVerifications { get; set; } = new List<IdentityVerification>();
        public IEnumerable<DisputeLog> ActiveDisputes { get; set; } = new List<DisputeLog>();
    }
}