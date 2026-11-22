using SalefnyMVC.Models.Entities;
using SalefnyMVC.Models.Enums;

namespace SalefnyMVC.ViewModels
{
    public class AdminEscrowViewModel
    {
        public string BookingIdFilter { get; set; } = string.Empty;
        public EscrowTransactionType? TypeFilter { get; set; }
        public List<EscrowTransaction> Transactions { get; set; } = new();

        public decimal TotalAmount { get; set; }
        public decimal HoldTotal { get; set; }
        public decimal ReleaseTotal { get; set; }
        public decimal FreezeTotal { get; set; }
        public decimal DeductTotal { get; set; }
        public decimal RefundTotal { get; set; }

        /// <summary>Cash still inside escrow for the filtered transactions.</summary>
        public decimal NetHeld => HoldTotal - ReleaseTotal - DeductTotal - RefundTotal;
    }
}
