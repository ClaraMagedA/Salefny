using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SalefnyMVC.Models.Entities
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; }

        [Required]
        [MaxLength(50)]
        public string City { get; set; } // e.g. Cairo, Giza, Alexandria

        public bool IsVerified { get; set; } = false;

        public decimal TrustRating { get; set; } = 5.0m;

        public decimal EscrowBalance { get; set; } = 0.0m;

        // Navigation properties
        public virtual ICollection<Item> Items { get; set; }
        public virtual ICollection<Booking> RentedBookings { get; set; }
        public virtual IdentityVerification IdentityVerification { get; set; }
    }
}