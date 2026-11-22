using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SalefnyMVC.Models.Enums;
using SalefnyMVC.Models;

namespace SalefnyMVC.Models.Entities
{
    public class IdentityVerification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        // NationalId or Passport, as captured from the uploaded document photo.
        [Required]
        [MaxLength(20)]
        public string DocumentType { get; set; } = "NationalId";

        // Name read from the document; server-checked against ApplicationUser.FullName.
        [Required]
        [MaxLength(100)]
        public string DocumentHolderName { get; set; } = string.Empty;

        // Holds either a 14-digit National ID or a passport number.
        [Required]
        [MaxLength(50)]
        public string NationalIdNumber { get; set; }

        [Required]
        public DateTime NationalIdExpiryDate { get; set; }

        [Required]
        public string FrontIdImagePath { get; set; }

        [Required]
        public string SelfieImagePath { get; set; }

        public VerificationStatus Status { get; set; } = VerificationStatus.Pending;

        [MaxLength(500)]
        public string? AdminNote { get; set; }

        public string? ReviewedByAdminId { get; set; }

        public DateTime? ReviewedAt { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}