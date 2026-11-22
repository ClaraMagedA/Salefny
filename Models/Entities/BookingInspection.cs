using SalefnyMVC.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalefnyMVC.Models.Entities
{
    public class BookingInspection
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BookingId { get; set; }

        [ForeignKey("BookingId")]
        public virtual Booking Booking { get; set; }

        public InspectionStage Stage { get; set; }

        [Required]
        [MaxLength(450)]
        public string RecordedBy { get; set; }

        [Required]
        public string PhotoPaths { get; set; } = string.Empty;

        public bool DamageReported { get; set; }

        [MaxLength(2000)]
        public string? DamageDescription { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
