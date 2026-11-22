using SalefnyMVC.Models.Entities;
using SalefnyMVC.Models.Enums;

namespace SalefnyMVC.ViewModels
{
    public class AdminVerificationsViewModel
    {
        public List<IdentityVerification> Verifications { get; set; } = new();

        public int PendingCount => Verifications.Count(v => v.Status == VerificationStatus.Pending);

        public string DocumentTypeLabel(string? documentType)
            => string.Equals(documentType, "Passport", StringComparison.OrdinalIgnoreCase)
                ? "جواز سفر"
                : "بطاقة رقم قومي";
    }
}
