using System.ComponentModel.DataAnnotations;

namespace SalefnyMVC.Models.ViewModels
{
    public class VerificationViewModel
    {
        [Required(ErrorMessage = "يرجى إدخال رقم الهاتف.")]
        [Phone(ErrorMessage = "صيغة رقم الهاتف غير صحيحة.")]
        [Display(Name = "رقم الهاتف المحمول")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "يرجى اختيار نوع المستند.")]
        [Display(Name = "نوع المستند")]
        public string DocumentType { get; set; } = "NationalId";

        [Required(ErrorMessage = "يرجى التقاط صورة واضحة للمستند.")]
        [Display(Name = "صورة المستند")]
        public IFormFile? DocumentImage { get; set; }

        [Required(ErrorMessage = "لم يتم التعرف على رقم المستند من الصورة.")]
        [Display(Name = "رقم المستند المقروء من الصورة")]
        public string DocumentNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "لم يتم التعرف على تاريخ انتهاء المستند من الصورة.")]
        [DataType(DataType.Date)]
        [Display(Name = "تاريخ انتهاء المستند المقروء من الصورة")]
        public DateTime ExpiryDate { get; set; } = DateTime.Today.AddYears(5);

        [Required(ErrorMessage = "لم يتم التعرف على الاسم الموجود على المستند.")]
        [Display(Name = "الاسم المقروء من المستند")]
        public string DocumentHolderName { get; set; } = string.Empty;
    }
}
