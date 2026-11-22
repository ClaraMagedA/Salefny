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
        public string? Description { get; set; }

        [MaxLength(50)]
        public string? IconClass { get; set; }

        public int? ParentCategoryId { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual Category? ParentCategory { get; set; }
        public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();
        public virtual ICollection<Item> Items { get; set; } = new List<Item>();
    }
}
