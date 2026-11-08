using System.ComponentModel.DataAnnotations;

namespace SalefnyMVC.Models.Entities
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } // e.g., معدات تصوير, معدات تخييم

        [MaxLength(255)]
        public string Description { get; set; }

        public virtual ICollection<Item> Items { get; set; }
    }
}