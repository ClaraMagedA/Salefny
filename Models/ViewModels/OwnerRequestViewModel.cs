using System.ComponentModel.DataAnnotations;

namespace SalefnyMVC.Models.ViewModels
{
    public class OwnerRequestViewModel
    {
        [Required(ErrorMessage = "يرجى إدخال عنوان المنتج.")]
        [MaxLength(150, ErrorMessage = "عنوان المنتج طويل جداً.")]
        [Display(Name = "عنوان المنتج")]
        public string ProductTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "يرجى اختيار قسم المنتج.")]
        [Display(Name = "قسم المنتج")]
        public int CategoryId { get; set; }

        [Display(Name = "المنتج متاح فوراً")]
        public bool AvailableNow { get; set; } = true;

        [DataType(DataType.Date)]
        [Display(Name = "متاح من تاريخ")]
        public DateTime? AvailableFrom { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "متاح إلى تاريخ")]
        public DateTime? AvailableTo { get; set; }

        [Required(ErrorMessage = "يرجى إدخال سعر اليوم.")]
        [Range(0.01, 1000000, ErrorMessage = "سعر اليوم يجب أن يكون أكبر من صفر.")]
        [Display(Name = "سعر اليوم (ج.م)")]
        public decimal PricePerDay { get; set; }

        [Range(0, 10000000, ErrorMessage = "قيمة التأمين غير صحيحة.")]
        [Display(Name = "مبلغ التأمين (ج.م) - اختياري")]
        public decimal SecurityDeposit { get; set; }

        [MaxLength(50, ErrorMessage = "اسم المدينة طويل جداً.")]
        [Display(Name = "المدينة")]
        public string City { get; set; } = string.Empty;

        [Display(Name = "وصف مختصر للحالة")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "صور الفحص الست")]
        public List<IFormFile>? AngleImages { get; set; }
    }
}
