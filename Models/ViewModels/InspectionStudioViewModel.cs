using SalefnyMVC.Models.Entities;
using SalefnyMVC.Models.Enums;

namespace SalefnyMVC.ViewModels
{
    public class InspectionStudioViewModel
    {
        public Booking Booking { get; set; } = null!;
        public InspectionStage Stage { get; set; }
        public bool IsRenter { get; set; }
        public bool DamageMode { get; set; }
        public BookingInspection? ExistingInspection { get; set; }

        public string StageLabel => Stage == InspectionStage.ReturnByRenter
            ? "فحص الإرجاع من المستأجر (6 زوايا)"
            : "فحص استلام الإرجاع من المالك (6 زوايا)";

        public string Subtitle => Stage == InspectionStage.ReturnByRenter
            ? "صوّر الغرض من الزوايا الست قبل إعادته إلى المالك. هذا الفحص يُستخدم لمطابقة حالة الغرض عند الاستلام."
            : "بعد وصول الغرض إليك، صوّر الزوايا الست مرة أخرى وسجّل أي ضرر قبل إطلاق الوديعة.";

        public bool CanSubmit => Stage == InspectionStage.ReturnByRenter
            ? Booking.IsDelivered
            : Booking.ReturnedAt != null;
    }
}
