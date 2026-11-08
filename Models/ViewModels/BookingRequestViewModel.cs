using System.ComponentModel.DataAnnotations;
using SalefnyMVC.Models.Entities;

namespace SalefnyMVC.ViewModels
{
    public class BookingRequestViewModel
    {
        public int ItemId { get; set; }
        public Item Item { get; set; }

        [Required(ErrorMessage = "تاريخ البدء مطلوب")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "تاريخ الإرجاع مطلوب")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required]
        public string DeliveryMethod { get; set; } // e.g., "Handover" or "Courier"

        public decimal TotalRentalAmount { get; set; }
        public decimal EscrowDepositAmount { get; set; }
        public decimal PlatformServiceFee { get; set; }
        public decimal GrandTotal => TotalRentalAmount + EscrowDepositAmount + PlatformServiceFee;
    }
}