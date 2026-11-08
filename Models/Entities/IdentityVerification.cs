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

        [Required]
        [StringLength(14, MinimumLength = 14, ErrorMessage = "الرقم القومي يجب أن يكون 14 رقماً")]
        public string NationalIdNumber { get; set; }

        [Required]
        public DateTime NationalIdExpiryDate { get; set; }

        [Required]
        public string FrontIdImagePath { get; set; }

        [Required]
        public string SelfieImagePath { get; set; }

        public VerificationStatus Status { get; set; } = VerificationStatus.Pending;

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}