using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SalefnyMVC.Data;
using SalefnyMVC.Models.Enums;
using SalefnyMVC.Models.Entities;
using SalefnyMVC.Models.Options;
using SalefnyMVC.Models.ViewModels;
using SalefnyMVC.Services;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SalefnyMVC.Controllers
{
    [Authorize]
    public class VerificationController : Controller
    {
        private const string OtpKey = "Verification.Otp";
        private const string OtpExpiryKey = "Verification.OtpExpiry";
        private const string PhoneKey = "Verification.Phone";
        private const string EmailKey = "Verification.Email";
        private const string DocTypeKey = "Verification.DocType";
        private const string DocNumberKey = "Verification.DocNumber";
        private const string DocExpiryKey = "Verification.DocExpiry";
        private const string DocNameKey = "Verification.DocName";
        private const string DocImageKey = "Verification.DocImage";
        private const string SmsSentKey = "Verification.SmsSent"; // "true" = OTP was really emailed

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SalefnyDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly EmailOptions _emailOptions;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<VerificationController> _logger;
        private readonly INotificationService _notifications;

        public VerificationController(
            UserManager<ApplicationUser> userManager,
            SalefnyDbContext context,
            IEmailSender emailSender,
            IOptions<EmailOptions> emailOptions,
            IWebHostEnvironment environment,
            ILogger<VerificationController> logger,
            INotificationService notifications)
        {
            _userManager = userManager;
            _context = context;
            _emailSender = emailSender;
            _emailOptions = emailOptions.Value;
            _environment = environment;
            _logger = logger;
            _notifications = notifications;
        }

        [HttpGet]
        public async Task<IActionResult> Verify()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null && user.IsVerified)
            {
                TempData["VerificationInfo"] = "هويتك موثقة بالفعل.";
                return RedirectToAction("Profile", "Account");
            }

            ViewBag.ProfileFullName = user?.FullName ?? string.Empty;
            return View(new VerificationViewModel
            {
                PhoneNumber = user?.PhoneNumber ?? string.Empty
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Verify(VerificationViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            ViewBag.ProfileFullName = user.FullName ?? string.Empty;

            if (!ModelState.IsValid) return View(model);

            if (model.DocumentType is not ("NationalId" or "Passport"))
            {
                ModelState.AddModelError(nameof(model.DocumentType), "اختر بطاقة الرقم القومي أو جواز السفر.");
                return View(model);
            }

            var isNationalId = model.DocumentType == "NationalId";
            var documentNumber = OtpNormalizer.Normalize(model.DocumentNumber).Replace(" ", string.Empty).Replace("-", string.Empty);

            if (isNationalId && !Regex.IsMatch(documentNumber, @"^\d{14}$"))
            {
                ModelState.AddModelError(nameof(model.DocumentNumber), "رقم المستند المقروء يجب أن يتكون من 14 رقماً. أعد التقاط الصورة أو عدّل القراءة.");
                return View(model);
            }

            if (!isNationalId && !Regex.IsMatch(documentNumber, @"^[A-Za-z0-9]{6,20}$"))
            {
                ModelState.AddModelError(nameof(model.DocumentNumber), "رقم جواز السفر يجب أن يكون بين 6 و20 حرفاً أو رقماً.");
                return View(model);
            }

            if (model.ExpiryDate <= DateTime.Today)
            {
                ModelState.AddModelError(nameof(model.ExpiryDate), "تاريخ انتهاء المستند المقروء يجب أن يكون في المستقبل.");
                return View(model);
            }

            var documentName = NormalizeIdentityName(model.DocumentHolderName);
            if (string.IsNullOrEmpty(documentName) ||
                !string.Equals(documentName, NormalizeIdentityName(user.FullName), StringComparison.Ordinal))
            {
                ModelState.AddModelError(nameof(model.DocumentHolderName),
                    $"الاسم المقروء من المستند يجب أن يطابق اسمك في الملف الشخصي ({user.FullName}).");
                return View(model);
            }

            if (model.DocumentImage == null || model.DocumentImage.Length == 0)
            {
                ModelState.AddModelError(nameof(model.DocumentImage), "يرجى التقاط صورة واضحة للمستند أو اختيار صورة منه.");
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                ModelState.AddModelError("", "لا يوجد بريد إلكتروني مسجل لحسابك لإرسال رمز التحقق إليه.");
                return View(model);
            }

            var upload = await SaveDocumentImageAsync(model.DocumentImage);
            if (upload.Path == null)
            {
                ModelState.AddModelError(nameof(model.DocumentImage), upload.Error ?? "تعذر حفظ صورة المستند.");
                return View(model);
            }

            var savedDocumentPath = upload.Path;

            // Generate a 6-digit one-time password and keep it in the server-side session.
            var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString(CultureInfo.InvariantCulture);
            HttpContext.Session.SetString(OtpKey, otp);
            HttpContext.Session.SetString(OtpExpiryKey, DateTime.UtcNow.AddMinutes(5).ToString("O", CultureInfo.InvariantCulture));
            HttpContext.Session.SetString(PhoneKey, model.PhoneNumber);
            HttpContext.Session.SetString(EmailKey, user.Email);
            HttpContext.Session.SetString(DocTypeKey, model.DocumentType);
            HttpContext.Session.SetString(DocNumberKey, documentNumber);
            HttpContext.Session.SetString(DocExpiryKey, model.ExpiryDate.ToString("O", CultureInfo.InvariantCulture));
            HttpContext.Session.SetString(DocNameKey, documentName);
            HttpContext.Session.SetString(DocImageKey, savedDocumentPath);

            ViewBag.OtpEmail = user.Email;

            if (!_emailOptions.IsConfigured)
            {
                // Clearly-labelled local fallback: no email is actually delivered.
                _logger.LogWarning("Email (SMTP) not configured. Demo verification OTP for {Email}: {Otp}", user.Email, otp);
                HttpContext.Session.SetString(SmsSentKey, "false");
                ViewBag.SmsSent = false;
                ViewBag.DemoOtp = otp;
                return View("ConfirmOtp");
            }

            var safeName = System.Net.WebUtility.HtmlEncode(user.FullName ?? string.Empty);
            var body = $@"<div dir=""rtl"" style=""font-family:Tahoma,Arial,sans-serif;max-width:480px;margin:auto;border:1px solid #e2e8f0;border-radius:12px;padding:24px;text-align:right"">
<h2 style=""color:#0d9488;margin-top:0"">سَلِّفني</h2>
<p>مرحباً {safeName}،</p>
<p>رمز توثيق هويتك هو:</p>
<p dir=""ltr"" style=""font-size:32px;font-weight:bold;letter-spacing:8px;text-align:center;color:#0f172a"">{otp}</p>
<p style=""color:#64748b;font-size:13px"">الرمز صالح لمدة 5 دقائق. إذا لم تطلب هذا الرمز فتجاهل الرسالة ولا تشاركه مع أحد.</p>
</div>";

            var sent = await _emailSender.SendEmailAsync(user.Email, "رمز توثيق هويتك - سَلِّفني", body);

            if (!sent)
            {
                DeleteDocumentImage(savedDocumentPath);
                ClearOtpSession();
                ModelState.AddModelError("", "تعذر إرسال رمز التحقق إلى بريدك الإلكتروني. حاول مرة أخرى.");
                ViewBag.SmsSent = false;
                ViewBag.DemoOtp = null;
                return View(model);
            }

            HttpContext.Session.SetString(SmsSentKey, "true");
            ViewBag.SmsSent = true;
            ViewBag.DemoOtp = null;
            return View("ConfirmOtp");
        }

        [HttpGet]
        public IActionResult ConfirmOtp()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString(OtpKey)))
                return RedirectToAction(nameof(Verify));

            ViewBag.PhoneNumber = HttpContext.Session.GetString(PhoneKey);
            ViewBag.OtpEmail = HttpContext.Session.GetString(EmailKey);
            var sent = HttpContext.Session.GetString(SmsSentKey) == "true";
            ViewBag.SmsSent = sent;
            ViewBag.DemoOtp = sent ? null : HttpContext.Session.GetString(OtpKey);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmOtp(string otp)
        {
            ViewBag.PhoneNumber = HttpContext.Session.GetString(PhoneKey);
            ViewBag.OtpEmail = HttpContext.Session.GetString(EmailKey);
            var smsSent = HttpContext.Session.GetString(SmsSentKey) == "true";
            ViewBag.SmsSent = smsSent;
            ViewBag.DemoOtp = smsSent ? null : HttpContext.Session.GetString(OtpKey);

            var expected = HttpContext.Session.GetString(OtpKey);
            var expiryRaw = HttpContext.Session.GetString(OtpExpiryKey);

            if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(otp))
            {
                ModelState.AddModelError("", "انتهت جلسة التحقق. يرجى بدء التوثيق من جديد.");
                return View();
            }

            if (!DateTime.TryParse(expiryRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expiry) || expiry < DateTime.UtcNow)
            {
                ModelState.AddModelError("", "انتهت صلاحية رمز التحقق. اطلب رمزاً جديداً.");
                return View();
            }

            if (!string.Equals(OtpNormalizer.Normalize(otp), expected, StringComparison.Ordinal))
            {
                ModelState.AddModelError("", "رمز التحقق غير صحيح. تأكد من الرمز المكون من 6 أرقام.");
                return View();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // The OTP confirms email ownership only. Identity badges are granted by an admin
            // after reviewing the submitted document, so IsVerified stays unchanged here.
            user.PhoneNumber = HttpContext.Session.GetString(PhoneKey);
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);

            var documentExpiryRaw = HttpContext.Session.GetString(DocExpiryKey) ?? DateTime.Today.AddYears(5).ToString("O", CultureInfo.InvariantCulture);
            var documentType = HttpContext.Session.GetString(DocTypeKey) ?? "NationalId";
            var documentHolderName = HttpContext.Session.GetString(DocNameKey) ?? string.Empty;
            var documentNumber = HttpContext.Session.GetString(DocNumberKey) ?? string.Empty;
            var documentImagePath = HttpContext.Session.GetString(DocImageKey) ?? "/images/verification/front-placeholder.svg";
            var documentExpiry = DateTime.Parse(documentExpiryRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

            // UserId is unique, so a rejected or resubmitted request updates the existing record.
            var verification = await _context.IdentityVerifications
                .FirstOrDefaultAsync(v => v.UserId == user.Id);
            if (verification == null)
            {
                verification = new IdentityVerification { UserId = user.Id };
                _context.IdentityVerifications.Add(verification);
            }

            verification.DocumentType = documentType;
            verification.DocumentHolderName = documentHolderName;
            verification.NationalIdNumber = documentNumber;
            verification.NationalIdExpiryDate = documentExpiry;
            verification.FrontIdImagePath = documentImagePath;
            verification.SelfieImagePath = "/images/verification/selfie-placeholder.svg";
            verification.Status = VerificationStatus.Pending;
            verification.AdminNote = null;
            verification.ReviewedByAdminId = null;
            verification.ReviewedAt = null;
            verification.SubmittedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _notifications.NotifyAdminsAsync(
                $"طلب توثيق هوية جديد قيد مراجعة الإدارة للمستخدم {user.FullName}.",
                "verification", "/Admin/Verifications");
            await _notifications.NotifyAsync(user.Id,
                "تم إرسال مستند الهوية: قيد مراجعة الإدارة.",
                "verification", "/Account/Profile");

            ClearOtpSession();
            TempData["VerificationSuccess"] = "تم تأكيد بريدك الإلكتروني وإرسال الهوية للمراجعة. الحالة قيد مراجعة الإدارة.";
            return RedirectToAction("Profile", "Account");
        }

        private async Task<(string? Path, string? Error)> SaveDocumentImageAsync(IFormFile file)
        {
            if (file.Length == 0)
                return (null, "صورة المستند فارغة.");

            if (file.Length > 5 * 1024 * 1024)
                return (null, "حجم صورة المستند يجب ألا يتجاوز 5 ميجابايت.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp"))
                return (null, "يرجى استخدام صورة بصيغة JPG أو PNG أو WebP.");

            var webRoot = _environment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRoot))
                return (null, "تعذر حفظ صورة المستند.");

            var folderName = Guid.NewGuid().ToString("N");
            var folder = Path.Combine(webRoot, "uploads", "verifications", folderName);
            Directory.CreateDirectory(folder);

            var fileName = "document" + extension;
            var relativePath = $"/uploads/verifications/{folderName}/{fileName}";

            try
            {
                using var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create);
                await file.CopyToAsync(stream);
                return (relativePath, null);
            }
            catch (Exception)
            {
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
                return (null, "تعذر حفظ صورة المستند. حاول مرة أخرى.");
            }
        }

        private void DeleteDocumentImage(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath) || string.IsNullOrWhiteSpace(_environment.WebRootPath)) return;

            var fullPath = Path.Combine(_environment.WebRootPath, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(fullPath)) return;

            System.IO.File.Delete(fullPath);
            var folder = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(folder) && !Directory.EnumerateFileSystemEntries(folder).Any())
                Directory.Delete(folder);
        }

        private static string NormalizeIdentityName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            var normalized = OtpNormalizer.Normalize(value);
            var builder = new StringBuilder();

            foreach (var c in normalized)
            {
                if (c == '\u0640') continue; // Arabic tatweel

                if (char.IsWhiteSpace(c))
                {
                    if (builder.Length > 0 && builder[^1] != ' ') builder.Append(' ');
                    continue;
                }

                var mapped = c switch
                {
                    '\u0622' or '\u0623' or '\u0625' => '\u0627', // آ أ إ => ا
                    '\u0629' => '\u0647',                           // ة => ه
                    _ => c
                };
                builder.Append(mapped);
            }

            return builder.ToString().Trim();
        }

        private void ClearOtpSession()
        {
            HttpContext.Session.Remove(OtpKey);
            HttpContext.Session.Remove(OtpExpiryKey);
            HttpContext.Session.Remove(PhoneKey);
            HttpContext.Session.Remove(EmailKey);
            HttpContext.Session.Remove(DocTypeKey);
            HttpContext.Session.Remove(DocNumberKey);
            HttpContext.Session.Remove(DocExpiryKey);
            HttpContext.Session.Remove(DocNameKey);
            HttpContext.Session.Remove(DocImageKey);
            HttpContext.Session.Remove(SmsSentKey);
        }
    }
}