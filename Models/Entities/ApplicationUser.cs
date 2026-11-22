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

        // Admin suspension. Suspended users cannot sign in and their open bookings are cancelled.
        public bool IsActive { get; set; } = true;

        [MaxLength(300)]
        public string? ProfileImagePath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int LateCancellationCount { get; set; } = 0;

        // 0 means the user has not been rated yet. A rating is only shown once
        // a real review exists; accounts must never start pre-rated at 5/5.
        public decimal TrustRating { get; set; } = 0.0m;

        // Average of received reviews. Kept in sync with TrustRating so the
        // documented field name and the existing UI remain compatible.
        public decimal AverageRating { get; set; } = 0.0m;

        public decimal EscrowBalance { get; set; } = 0.0m;

        // Navigation properties
        public virtual ICollection<Item> Items { get; set; }
        public virtual ICollection<Booking> RentedBookings { get; set; }
        public virtual IdentityVerification IdentityVerification { get; set; }
    }
}