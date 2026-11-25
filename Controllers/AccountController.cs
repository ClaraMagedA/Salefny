using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalefnyMVC.Data;
using SalefnyMVC.Models.Entities;
using SalefnyMVC.Models.Enums;
using SalefnyMVC.Models.ViewModels;
using SalefnyMVC.Services;
using System.Globalization;
using System.Security.Cryptography;

namespace SalefnyMVC.Controllers
{
    public class AccountController : Controller
    {
        private const string LoginOtpKey = "Login.Otp";
        private const string LoginOtpExpiryKey = "Login.OtpExpiry";
        private const string LoginUserIdKey = "Login.UserId";
        private const string LoginEmailKey = "Login.Email";
        private const string LoginSentKey = "Login.Sent";
        private const string LoginReturnUrlKey = "Login.ReturnUrl";

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly SalefnyDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AccountController> _logger;
        private readonly IWebHostEnvironment _environment;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            SalefnyDbContext context,
            IEmailSender emailSender,
            ILogger<AccountController> logger,
            IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _emailSender = emailSender;
            _logger = logger;
            _environment = environment;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                HttpContext.Session.SetString(LoginReturnUrlKey, returnUrl);
            else
                HttpContext.Session.Remove(LoginReturnUrlKey);

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                HttpContext.Session.SetString(LoginReturnUrlKey, returnUrl);
            ViewBag.ReturnUrl = returnUrl;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "يرجى إدخال البريد الإلكتروني وكلمة المرور.");
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || !(await _signInManager.CheckPasswordSignInAsync(user, password, false)).Succeeded)
            {
                ModelState.AddModelError("", "بيانات الدخول غير صحيحة.");
                return View();
            }

            // Password is correct — issue an email OTP instead of signing in immediately.
            var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString(CultureInfo.InvariantCulture);
            HttpContext.Session.SetString(LoginOtpKey, otp);
            HttpContext.Session.SetString(LoginOtpExpiryKey, DateTime.UtcNow.AddMinutes(5).ToString("O", CultureInfo.InvariantCulture));
            HttpContext.Session.SetString(LoginUserIdKey, user.Id);
            HttpContext.Session.SetString(LoginEmailKey, user.Email ?? email);

            var body = "<div dir='rtl' style='font-family:Cairo,sans-serif'>" +
                       "<h3>سَلِّفني — رمز تحقق تسجيل الدخول</h3>" +
                       "<p>أدخل الرمز التالي لإكمال تسجيل الدخول. وهو صالح لمدة 5 دقائق.</p>" +
                       $"<div style='font-size:28px;font-weight:800;letter-spacing:8px' dir='ltr'>{otp}</div>" +
                       "<p class='text-muted'>إذا لم تطلب تسجيل الدخول، تجاهل هذه الرسالة.</p></div>";

            var sent = await _emailSender.SendEmailAsync(user.Email ?? email, "رمز تحقق تسجيل الدخول - سَلِّفني", body);
            HttpContext.Session.SetString(LoginSentKey, sent ? "true" : "false");

            if (!sent)
            {
                _logger.LogWarning("Failed to send login OTP email to {Email}.", user.Email);
                ModelState.AddModelError("", "تعذر إرسال رمز التحقق إلى بريدك الإلكتروني حالياً. يرجى المحاولة لاحقاً.");
                return View();
            }

            ViewBag.Email = user.Email;
            return View("LoginOtp");
        }

        [HttpGet]
        public IActionResult LoginOtp()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString(LoginOtpKey)))
                return RedirectToAction(nameof(Login));

            ViewBag.Email = HttpContext.Session.GetString(LoginEmailKey);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginOtp(string otp)
        {
            ViewBag.Email = HttpContext.Session.GetString(LoginEmailKey);

            var expected = HttpContext.Session.GetString(LoginOtpKey);
            var expiryRaw = HttpContext.Session.GetString(LoginOtpExpiryKey);
            var userId = HttpContext.Session.GetString(LoginUserIdKey);

            if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(userId))
            {
                ModelState.AddModelError("", "انتهت جلسة التحقق. سجّل الدخول من جديد.");
                return View();
            }

            if (!DateTime.TryParse(expiryRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expiry) || expiry < DateTime.UtcNow)
            {
                ModelState.AddModelError("", "انتهت صلاحية رمز التحقق. سجّل الدخول للحصول على رمز جديد.");
                return View();
            }

            if (!string.Equals(OtpNormalizer.Normalize(otp), expected, StringComparison.Ordinal))
            {
                ModelState.AddModelError("", "رمز التحقق غير صحيح. تأكد من الرمز المكون من 6 أرقام.");
                return View();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ModelState.AddModelError("", "تعذر العثور على الحساب. سجّل الدخول من جديد.");
                return View();
            }

            var isAdmin = await _userManager.IsInRoleAsync(user, AppRoles.Admin)
                          || await _userManager.IsInRoleAsync(user, AppRoles.SuperAdmin);
            var returnUrl = HttpContext.Session.GetString(LoginReturnUrlKey);

            await _signInManager.SignInAsync(user, isPersistent: false);

            HttpContext.Session.Remove(LoginOtpKey);
            HttpContext.Session.Remove(LoginOtpExpiryKey);
            HttpContext.Session.Remove(LoginUserIdKey);
            HttpContext.Session.Remove(LoginEmailKey);
            HttpContext.Session.Remove(LoginSentKey);
            HttpContext.Session.Remove(LoginReturnUrlKey);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return isAdmin
                ? RedirectToAction("Dashboard", "Admin")
                : RedirectToAction("Profile");
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(string fullName, string email, string password, string confirmPassword, string city)
        {
            ViewBag.FullName = fullName;
            ViewBag.Email = email;
            ViewBag.City = city;

            if (password != confirmPassword)
            {
                ModelState.AddModelError("", "كلمة المرور وتأكيدها غير متطابقين.");
                return View();
            }

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(city))
            {
                ModelState.AddModelError("", "يرجى إدخال جميع البيانات المطلوبة.");
                return View();
            }

            var user = new ApplicationUser { UserName = email, Email = email, FullName = fullName, City = city };
            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                var roleResult = await _userManager.AddToRoleAsync(user, AppRoles.Renter);
                if (!roleResult.Succeeded)
                    _logger.LogError("Could not assign Renter role to {Email}: {Errors}",
                        email, string.Join(", ", roleResult.Errors.Select(e => e.Description)));

                // Send a confirmation link, but keep the demo flow working: sign-in is immediate.
                var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationUrl = Url.Action("ConfirmEmail", "Account",
                    new { userId = user.Id, token = confirmationToken })!;
                var confirmationBody = "<div dir='rtl' style='font-family:Cairo,sans-serif'>" +
                                       "<h3>سَلِّفني — تأكيد البريد الإلكتروني</h3>" +
                                       "<p>أهلًا بك. أكّد بريدك الإلكتروني بالضغط على الرابط التالي:</p>" +
                                       $"<p><a href='{System.Net.WebUtility.HtmlEncode(confirmationUrl)}'>تأكيد البريد الإلكتروني</a></p>" +
                                       "<p class='text-muted'>إذا لم تنشئ هذا الحساب، تجاهل هذه الرسالة.</p></div>";
                var confirmationSent = await _emailSender.SendEmailAsync(
                    user.Email ?? email, "تأكيد البريد الإلكتروني - سَلِّفني", confirmationBody);
                if (!confirmationSent)
                {
                    _logger.LogWarning("SMTP is not configured. Demo confirmation link for {Email}: {Url}",
                        user.Email, confirmationUrl);
                    TempData["DemoConfirmLink"] = confirmationUrl;
                }

                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Profile");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", TranslateIdentityError(error));
            }
            return View();
        }

        private static string TranslateIdentityError(IdentityError error) => error.Code switch
        {
            "PasswordTooShort" => "كلمة المرور يجب ألا تقل عن 6 أحرف.",
            "PasswordRequiresDigit" => "كلمة المرور يجب أن تحتوي على رقم واحد على الأقل.",
            "DuplicateUserName" or "DuplicateEmail" => "البريد الإلكتروني مستخدم بالفعل. جرّب بريداً آخر.",
            "InvalidEmail" => "صيغة البريد الإلكتروني غير صحيحة.",
            _ => error.Description
        };

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            ViewBag.Email = email;

            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError("", "يرجى إدخال بريدك الإلكتروني.");
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Do not reveal whether an account exists for this address.
                ViewBag.Sent = true;
                ViewBag.Message = "إذا كان هذا البريد مسجلاً لدينا، فستصلك رسالة إعادة تعيين كلمة المرور.";
                return View();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetUrl = Url.Action("ResetPassword", "Account",
                new { userId = user.Id, token = token })!;
            var body = "<div dir='rtl' style='font-family:Cairo,sans-serif'>" +
                       "<h3>سَلِّفني — إعادة تعيين كلمة المرور</h3>" +
                       "<p>تلقينا طلباً لإعادة تعيين كلمة المرور لحسابك. اضغط على الرابط التالي:</p>" +
                       $"<p><a href='{System.Net.WebUtility.HtmlEncode(resetUrl)}'>إعادة تعيين كلمة المرور</a></p>" +
                       "<p class='text-muted'>إذا لم تطلب ذلك، تجاهل هذه الرسالة ولن يتغير شيء.</p></div>";

            var sent = await _emailSender.SendEmailAsync(
                user.Email ?? email, "إعادة تعيين كلمة المرور - سَلِّفني", body);

            ViewBag.Sent = sent;
            ViewBag.DemoResetLink = sent ? null : resetUrl;
            ViewBag.Message = sent
                ? "أرسلنا رابط إعادة تعيين كلمة المرور إلى بريدك الإلكتروني."
                : null;

            if (!sent)
                _logger.LogWarning("SMTP is not configured. Demo password reset link for {Email}: {Url}", user.Email, resetUrl);

            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string? userId, string? token)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
            {
                TempData["Error"] = "رابط إعادة تعيين كلمة المرور غير صالح.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            ViewBag.UserId = userId;
            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string userId, string token, string newPassword, string confirmPassword)
        {
            ViewBag.UserId = userId;
            ViewBag.Token = token;

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "كلمة المرور وتأكيدها غير متطابقين.");
                return View();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ModelState.AddModelError("", "رابط إعادة التعيين غير صالح أو منتهي. اطلب رابطاً جديداً.");
                return View();
            }

            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", TranslateIdentityError(error));
                return View();
            }

            TempData["Success"] = "تم تغيير كلمة المرور بنجاح. سجّل الدخول بكلمة المرور الجديدة.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string? userId, string? token)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
                return View(false);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return View(false);

            ViewBag.Email = user.Email;
            var result = await _userManager.ConfirmEmailAsync(user, token);
            return View(result.Succeeded);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            await SetNotificationCountAsync(user.Id);
            return View(user);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(string fullName, string city, string? phoneNumber, IFormFile? profileImage)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (string.IsNullOrWhiteSpace(fullName) || fullName.Trim().Length > 100)
                ModelState.AddModelError(nameof(fullName), "الاسم مطلوب ويجب ألا يتجاوز 100 حرف.");
            if (string.IsNullOrWhiteSpace(city) || city.Trim().Length > 50)
                ModelState.AddModelError(nameof(city), "المحافظة مطلوبة ويجب ألا تتجاوز 50 حرفاً.");
            if (!string.IsNullOrWhiteSpace(phoneNumber) && phoneNumber.Trim().Length > 50)
                ModelState.AddModelError(nameof(phoneNumber), "رقم الهاتف يجب ألا يتجاوز 50 حرفاً.");

            string? extension = null;
            if (profileImage is { Length: > 0 })
            {
                extension = Path.GetExtension(profileImage.FileName).ToLowerInvariant();
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                if (!allowed.Contains(extension))
                    ModelState.AddModelError(nameof(profileImage), "صورة الملف الشخصي يجب أن تكون بصيغة JPG أو PNG أو WebP.");
                else if (profileImage.Length > 5 * 1024 * 1024)
                    ModelState.AddModelError(nameof(profileImage), "حجم الصورة يجب ألا يتجاوز 5 ميجابايت.");
            }

            if (!ModelState.IsValid)
            {
                await SetNotificationCountAsync(user.Id);
                return View(user);
            }

            string? newImagePath = null;
            if (profileImage is { Length: > 0 } && extension != null)
            {
                var folder = Path.Combine(_environment.WebRootPath ?? string.Empty, "uploads", "profiles", user.Id);
                try
                {
                    Directory.CreateDirectory(folder);
                    var fileName = $"{Guid.NewGuid():N}{extension}";
                    using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
                    {
                        await profileImage.CopyToAsync(stream);
                    }
                    newImagePath = $"/uploads/profiles/{user.Id}/{fileName}";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Could not save profile image for user {UserId}", user.Id);
                    ModelState.AddModelError("", "تعذر حفظ الصورة. حاول مرة أخرى.");
                    await SetNotificationCountAsync(user.Id);
                    return View(user);
                }
            }

            user.FullName = fullName.Trim();
            user.City = city.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
            if (newImagePath != null)
                user.ProfileImagePath = newImagePath;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                    ModelState.AddModelError("", error.Description);
                await SetNotificationCountAsync(user.Id);
                return View(user);
            }
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حفظ بياناتك الشخصية بنجاح.";
            return RedirectToAction(nameof(Profile));
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> PublicProfile(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["Error"] = "لم يتم تحديد المستخدم المطلوب.";
                return RedirectToAction("Index", "Items");
            }

            var profileUser = await _userManager.FindByIdAsync(id);
            if (profileUser == null)
            {
                TempData["Error"] = "لم يتم العثور على هذا المستخدم.";
                return RedirectToAction("Index", "Items");
            }

            ViewBag.Listings = await _context.Items
                .Include(i => i.Category)
                .Where(i => i.OwnerId == id && i.IsActive && i.ApprovalStatus == ListingApprovalStatus.Approved)
                .OrderByDescending(i => i.Id)
                .ToListAsync();

            ViewBag.Reviews = await _context.Reviews
                .Include(r => r.Reviewer)
                .Include(r => r.Item)
                .Where(r => r.RevieweeId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var currentUserId = _userManager.GetUserId(User);
            ViewBag.IsOwnProfile = currentUserId == id;
            await SetNotificationCountAsync(currentUserId);

            // Phone and e-mail are deliberately never placed in ViewBag for other users.
            return View(profileUser);
        }

        private async Task SetNotificationCountAsync(string? userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                ViewBag.NotificationCount = 0;
                return;
            }

            ViewBag.NotificationCount = await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> OwnerRequest()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (await _userManager.IsInRoleAsync(user, AppRoles.Owner))
            {
                TempData["VerificationInfo"] = "حسابك مفعّل ك مالك بالفعل، يمكنك عرض أغراضك مباشرة.";
                return RedirectToAction("Profile");
            }

            var request = await _context.OwnerRequests
                .Include(r => r.Category)
                .Include(r => r.Images)
                .Where(r => r.UserId == user.Id)
                .OrderByDescending(r => r.Id)
                .FirstOrDefaultAsync();

            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.PendingRequest = request?.Status == OwnerRequestStatus.Pending;
            ViewBag.LastNote = request?.AdminNote;
            ViewBag.SubmittedRequest = request;

            var model = new OwnerRequestViewModel
            {
                ProductTitle = request?.ProductTitle ?? string.Empty,
                CategoryId = request?.CategoryId ?? 0,
                AvailableNow = request?.AvailableNow ?? true,
                AvailableFrom = request?.AvailableFrom,
                AvailableTo = request?.AvailableTo,
                PricePerDay = request?.PricePerDay ?? 0,
                SecurityDeposit = request?.SecurityDeposit ?? 0,
                City = request?.City ?? user.City ?? string.Empty,
                Description = request?.Description ?? string.Empty
            };
            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OwnerRequest(OwnerRequestViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (await _userManager.IsInRoleAsync(user, AppRoles.Owner))
                return RedirectToAction("Profile");

            var alreadyPending = await _context.OwnerRequests
                .AnyAsync(r => r.UserId == user.Id && r.Status == OwnerRequestStatus.Pending);
            if (alreadyPending)
            {
                TempData["VerificationInfo"] = "طلبك قيد المراجعة بالفعل. سيتم إخطارك بعد اتخاذ الإدارة قراراً.";
                return RedirectToAction(nameof(OwnerRequest));
            }

            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.PendingRequest = false;
            ViewBag.LastNote = null;
            ViewBag.SubmittedRequest = null;

            if (!await _context.Categories.AnyAsync(c => c.Id == model.CategoryId))
                ModelState.AddModelError(nameof(model.CategoryId), "يرجى اختيار قسم صحيح.");

            if (!model.AvailableNow)
            {
                if (!model.AvailableFrom.HasValue || !model.AvailableTo.HasValue)
                    ModelState.AddModelError("", "يرجى تحديد تاريخي بداية ونهاية توفر المنتج، أو اختيار «متاح فوراً».");
                else if (model.AvailableTo.Value.Date < model.AvailableFrom.Value.Date)
                    ModelState.AddModelError(nameof(model.AvailableTo), "تاريخ نهاية التوفر يجب أن يكون بعد تاريخ البداية.");
            }

            if (model.AngleImages == null || model.AngleImages.Count != 6 ||
                model.AngleImages.Any(f => f == null || f.Length == 0))
            {
                ModelState.AddModelError(nameof(model.AngleImages), "يرجى رفع 6 صور واضحة للمنتج من الزوايا الست.");
            }
            else if (model.AngleImages.Any(f => Path.GetExtension(f.FileName).ToLowerInvariant() is not (".jpg" or ".jpeg" or ".png" or ".webp")))
            {
                ModelState.AddModelError(nameof(model.AngleImages), "صور المنتج يجب أن تكون بصيغة JPG أو PNG أو WebP.");
            }
            else if (model.AngleImages.Any(f => f.Length > 5 * 1024 * 1024))
            {
                ModelState.AddModelError(nameof(model.AngleImages), "حجم كل صورة يجب ألا يتجاوز 5 ميجابايت.");
            }

            if (!ModelState.IsValid)
                return View(model);

            var savedPaths = new List<string>();
            var folderName = Guid.NewGuid().ToString("N");
            var folder = Path.Combine(_environment.WebRootPath, "uploads", "owner-requests", folderName);
            Directory.CreateDirectory(folder);

            try
            {
                for (var i = 0; i < 6; i++)
                {
                    var extension = Path.GetExtension(model.AngleImages[i].FileName).ToLowerInvariant();
                    var fileName = $"angle-{i + 1}{extension}";
                    using var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create);
                    await model.AngleImages[i].CopyToAsync(stream);
                    savedPaths.Add($"/uploads/owner-requests/{folderName}/{fileName}");
                }
            }
            catch (Exception)
            {
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
                ModelState.AddModelError("", "تعذر حفظ صور المنتج. حاول مرة أخرى.");
                return View(model);
            }

            var request = new OwnerRequest
            {
                UserId = user.Id,
                Status = OwnerRequestStatus.Pending,
                RequestedAt = DateTime.UtcNow,
                ProductTitle = model.ProductTitle.Trim(),
                CategoryId = model.CategoryId,
                AvailableNow = model.AvailableNow,
                AvailableFrom = model.AvailableNow ? null : model.AvailableFrom?.Date,
                AvailableTo = model.AvailableNow ? null : model.AvailableTo?.Date,
                PricePerDay = model.PricePerDay,
                SecurityDeposit = model.SecurityDeposit,
                City = string.IsNullOrWhiteSpace(model.City) ? user.City ?? string.Empty : model.City.Trim(),
                Description = model.Description?.Trim() ?? string.Empty,
                Images = savedPaths.Select((path, index) => new OwnerRequestImage
                {
                    AngleIndex = index + 1,
                    ImagePath = path
                }).ToList()
            };

            _context.OwnerRequests.Add(request);
            await _context.SaveChangesAsync();

            TempData["VerificationSuccess"] = "تم إرسال المنتج مع الصور الست إلى الإدارة. ستراجعه قبل تفعيل صلاحية المالك ونشره في السوق.";
            return RedirectToAction(nameof(OwnerRequest));
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            await SetNotificationCountAsync(user.Id);
            return View(user);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MyListings()
        {
            var userId = _userManager.GetUserId(User);
            var listings = await _context.Items
                .Include(i => i.Category)
                .Where(i => i.OwnerId == userId)
                .ToListAsync();

            return View(listings);
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
