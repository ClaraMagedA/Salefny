using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalefnyMVC.Models.Entities
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public int BookingId { get; set; }

        [ForeignKey("BookingId")]
        public virtual Booking Booking { get; set; } = null!;

        // Kept for item-detail queries; always copied from Booking.ItemId.
        public int? ItemId { get; set; }

        [ForeignKey("ItemId")]
        public virtual Item? Item { get; set; }

        [Required]
        public string ReviewerId { get; set; } = string.Empty;

        [ForeignKey("ReviewerId")]
        public virtual ApplicationUser Reviewer { get; set; } = null!;

        [Required]
        public string RevieweeId { get; set; } = string.Empty;

        [ForeignKey("RevieweeId")]
        public virtual ApplicationUser Reviewee { get; set; } = null!;
    }
}
