using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SalefnyMVC.Models.Enums;

namespace SalefnyMVC.Models.Entities
{
    public class EscrowTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BookingId { get; set; }

        [ForeignKey("BookingId")]
        public virtual Booking Booking { get; set; } = null!;

        public EscrowTransactionType Type { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        [MaxLength(450)]
        public string? CreatedByAdminId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
