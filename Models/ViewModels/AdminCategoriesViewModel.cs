using System.ComponentModel.DataAnnotations;
using SalefnyMVC.Models.Entities;

namespace SalefnyMVC.ViewModels
{
    public class AdminCategoriesViewModel
    {
        public List<AdminCategoryRow> Categories { get; set; } = new();
        public List<Category> ParentOptions { get; set; } = new();
        public AdminCategoryForm CreateForm { get; set; } = new();
    }

    public class AdminCategoryRow
    {
        public Category Category { get; set; } = new();
        public string? ParentName { get; set; }
        public int ItemsCount { get; set; }
        public int SubCategoriesCount { get; set; }

        public bool CanDelete => ItemsCount == 0 && SubCategoriesCount == 0;
    }

    public class AdminCategoryForm
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم التصنيف مطلوب.")]
        [MaxLength(100, ErrorMessage = "اسم التصنيف لا يتجاوز 100 حرف.")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(255, ErrorMessage = "الوصف لا يتجاوز 255 حرف.")]
        public string? Description { get; set; }

        [MaxLength(50, ErrorMessage = "اسم أيقونة Font Awesome لا يتجاوز 50 حرف.")]
        public string? IconClass { get; set; }

        public int? ParentCategoryId { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
