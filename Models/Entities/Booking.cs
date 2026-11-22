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

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalRentalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal EscrowDepositAmount { get; set; }

        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        public DepositStatus DepositStatus { get; set; } = DepositStatus.NotHeld;

        public DateTime? UpdatedAt { get; set; }

        [MaxLength(500)]
        public string? RenterNote { get; set; }

        [MaxLength(300)]
        public string? CancellationReason { get; set; }

        public bool LateCancellationByRenter { get; set; } = false;

        [MaxLength(50)]
        public string? DeliveryMethod { get; set; }

        [MaxLength(300)]
        public string? DeliveryDetails { get; set; }

        [MaxLength(50)]
        public string? PaymentMethod { get; set; }

        [MaxLength(200)]
        public string? PaymentReference { get; set; }

        public bool IsDelivered { get; set; } = false;

        public DateTime? DeliveredAt { get; set; }

        public DateTime? ReturnedAt { get; set; }

        [Required]
        public int ItemId { get; set; }

        [ForeignKey("ItemId")]
        public virtual Item Item { get; set; }

        [Required]
        public string RenterId { get; set; }

        [ForeignKey("RenterId")]
        public virtual ApplicationUser Renter { get; set; }

        public virtual DisputeLog? DisputeLog { get; set; }
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<BookingInspection> Inspections { get; set; } = new List<BookingInspection>();
        public virtual ICollection<EscrowTransaction> EscrowTransactions { get; set; } = new List<EscrowTransaction>();
    }
}