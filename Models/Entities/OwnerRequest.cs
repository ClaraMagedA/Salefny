using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SalefnyMVC.Models.Enums;

namespace SalefnyMVC.Models.Entities
{
    public class OwnerRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        public OwnerRequestStatus Status { get; set; } = OwnerRequestStatus.Pending;

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ReviewedAt { get; set; }

        [MaxLength(500)]
        public string? AdminNote { get; set; }

        // The product the applicant wants to list. Approval grants the Owner role
        // and publishes this product after the admin's quality check.
        [Required]
        [MaxLength(150)]
        public string ProductTitle { get; set; } = string.Empty;

        [Required]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; } = null!;

        public bool AvailableNow { get; set; } = true;

        public DateTime? AvailableFrom { get; set; }

        public DateTime? AvailableTo { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PricePerDay { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SecurityDeposit { get; set; }

        [MaxLength(50)]
        public string City { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int? CreatedItemId { get; set; }

        public virtual ICollection<OwnerRequestImage> Images { get; set; } = new List<OwnerRequestImage>();
    }
}
