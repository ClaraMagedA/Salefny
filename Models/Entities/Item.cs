using Microsoft.AspNetCore.Mvc.ViewEngines;
using SalefnyMVC.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalefnyMVC.Models.Entities
{
    public class Item
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PricePerDay { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SecurityDeposit { get; set; }

        [Required]
        [MaxLength(50)]
        public string City { get; set; }

        [MaxLength(50)]
        public string? Condition { get; set; }

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = false;

        // New listings are sent to the admin for a physical/photo quality check
        // before they are allowed in the public souq.
        public ListingApprovalStatus ApprovalStatus { get; set; } = ListingApprovalStatus.Pending;

        public bool PhysicallyVerified { get; set; }
        public bool HasSixClearPhotos { get; set; }
        public bool IsUndamaged { get; set; }
        public bool QualityAccepted { get; set; }

        public DateTime? ReviewedAt { get; set; }

        [MaxLength(500)]
        public string? AdminNote { get; set; }

        [Required]
        public string OwnerId { get; set; }

        [ForeignKey("OwnerId")]
        public virtual ApplicationUser Owner { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; }

        public virtual ICollection<ItemImage> ItemImages { get; set; }
        public virtual ICollection<AvailabilityBlock> AvailabilityBlocks { get; set; }
        public virtual ICollection<Booking> Bookings { get; set; }
        public virtual ICollection<Review> Reviews { get; set; }
    }
}
