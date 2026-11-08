using Microsoft.AspNetCore.Mvc.ViewEngines;
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
        [Column(TypeName = "decimal(18,2)]")]
        public decimal PricePerDay { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)]")]
        public decimal SecurityDeposit { get; set; }

        [Required]
        [MaxLength(50)]
        public string City { get; set; }

        public bool IsActive { get; set; } = true;

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
