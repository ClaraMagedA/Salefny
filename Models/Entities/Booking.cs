using SalefnyMVC.Models.Enums;
using SalefnyMVC.Models.Entities;
using SalefnyMVC.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalefnyMVC.Models.Entities
{
    public class Booking
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Column(TypeName = "decimal(18,2)]")]
        public decimal TotalRentalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)]")]
        public decimal EscrowDepositAmount { get; set; }

        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        [Required]
        public int ItemId { get; set; }

        [ForeignKey("ItemId")]
        public virtual Item Item { get; set; }

        [Required]
        public string RenterId { get; set; }

        [ForeignKey("RenterId")]
        public virtual ApplicationUser Renter { get; set; }

        public virtual DisputeLog DisputeLog { get; set; }
        public virtual Review Review { get; set; }
    }
}