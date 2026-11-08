using SalefnyMVC.Models.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalefnyMVC.Models.Entities
{
    public class DisputeLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ClaimDescription { get; set; }

        [Required]
        public string ReturnAnglePhotoPaths { get; set; } // JSON or comma-separated paths for the 6-angle return inspection

        public bool IsResolved { get; set; } = false;

        [Column(TypeName = "decimal(18,2)]")]
        public decimal DeductedDepositAmount { get; set; } = 0.0m;

        public DateTime LoggedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public int BookingId { get; set; }

        [ForeignKey("BookingId")]
        public virtual Booking Booking { get; set; }
    }
}