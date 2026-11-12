using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SalefnyMVC.Data;
using SalefnyMVC.Models.Entities;
using SalefnyMVC.Models.Enums;

namespace SalefnyMVC.Services;

public partial class VerificationService : IVerificationService
{
    private const string AdminRoleNormalized = "ADMIN";
    private const int MinimumAge = 18;
    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png" };

    private readonly SalefnyDbContext _db;
    private readonly IEmailService _email;
    private readonly ILogger<VerificationService> _logger;

    public VerificationService(SalefnyDbContext db, IEmailService email, ILogger<VerificationService> logger)
    {
        _db = db;
        _email = email;
        _logger = logger;
    }

    // [0-9] (not \d, which also matches Arabic-Indic digits) and \z (not $, which allows a trailing newline).
    [GeneratedRegex(@"^[0-9]{14}\z")]
    private static partial Regex NationalIdRegex();

    // ───────────────────────── Submit ─────────────────────────

    public async Task<ServiceResult> SubmitAsync(string userId, string nationalId, DateTime expiryDate,
        string frontIdImagePath, string selfieImagePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return ServiceResult.Fail("المستخدم غير معروف.");

        nationalId = nationalId?.Trim() ?? string.Empty;

        if (!TryParseNationalId(nationalId, out var birthDate))
            return ServiceResult.Fail("الرقم القومي غير صالح.");
        if (birthDate > DateTime.UtcNow.Date.AddYears(-MinimumAge))
            return ServiceResult.Fail($"يجب ألا يقل عمرك عن {MinimumAge} سنة.");
        if (expiryDate.Date <= DateTime.UtcNow.Date)
            return ServiceResult.Fail("بطاقة الهوية منتهية الصلاحية.");
        if (!IsSafeImagePath(frontIdImagePath) || !IsSafeImagePath(selfieImagePath))
            return ServiceResult.Fail("صورة البطاقة والصورة الشخصية مطلوبتان بصيغة JPG أو PNG.");
        if (string.Equals(frontIdImagePath, selfieImagePath, StringComparison.OrdinalIgnoreCase))
            return ServiceResult.Fail("يجب أن تكون صورة البطاقة مختلفة عن الصورة الشخصية.");

        var userExists = await _db.Users.AsNoTracking().AnyAsync(u => u.Id == userId, ct);
        if (!userExists)
            return ServiceResult.Fail("المستخدم غير موجود.");

        var usedByAnother = await _db.IdentityVerifications.AsNoTracking()
            .AnyAsync(v => v.NationalIdNumber == nationalId && v.UserId != userId, ct);
        if (usedByAnother)
            return ServiceResult.Fail("هذا الرقم القومي مسجل على حساب آخر.");

        var existing = await _db.IdentityVerifications.FirstOrDefaultAsync(v => v.UserId == userId, ct);

        if (existing is null)
        {
            _db.IdentityVerifications.Add(new IdentityVerification
            {
                UserId = userId,
                NationalIdNumber = nationalId,
                NationalIdExpiryDate = expiryDate,
                FrontIdImagePath = frontIdImagePath,
                SelfieImagePath = selfieImagePath,
                Status = VerificationStatus.Pending,
                SubmittedAt = DateTime.UtcNow
            });
        }
        else if (existing.Status == VerificationStatus.Approved)
        {
            return ServiceResult.Fail("هويتك موثقة بالفعل.");
        }
        else if (existing.Status == VerificationStatus.Pending)
        {
            return ServiceResult.Fail("طلب التوثيق قيد المراجعة.");
        }
        else
        {
            // Rejected: allow resubmission by reusing the single record (one verification per user).
            existing.NationalIdNumber = nationalId;
            existing.NationalIdExpiryDate = expiryDate;
            existing.FrontIdImagePath = frontIdImagePath;
            existing.SelfieImagePath = selfieImagePath;
            existing.Status = VerificationStatus.Pending;
            existing.SubmittedAt = DateTime.UtcNow;
        }

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            // Unique index on NationalIdNumber / UserId lost a race with a concurrent request.
            _logger.LogWarning(ex, "Verification submit conflict for user {UserId}", userId);
            return ServiceResult.Fail("تعذر إرسال الطلب، تأكد من بياناتك وحاول مرة أخرى.");
        }

        return ServiceResult.Ok();
    }

    // ───────────────────────── Review (admin only) ─────────────────────────

    public Task<ServiceResult> ApproveAsync(int verificationId, string adminId, CancellationToken ct = default) =>
        ReviewAsync(verificationId, adminId, approve: true, reason: null, ct);

    public Task<ServiceResult> RejectAsync(int verificationId, string adminId, string? reason, CancellationToken ct = default) =>
        ReviewAsync(verificationId, adminId, approve: false, reason, ct);

    public async Task<IReadOnlyList<IdentityVerification>> GetPendingAsync(string adminId, CancellationToken ct = default)
    {
        if (!await IsAdminAsync(adminId, ct))
        {
            _logger.LogWarning("Non-admin {UserId} requested the verification queue", adminId);
            throw new UnauthorizedAccessException();
        }

        return await _db.IdentityVerifications.AsNoTracking()
            .Include(v => v.User)
            .Where(v => v.Status == VerificationStatus.Pending)
            .OrderBy(v => v.SubmittedAt)           // oldest first (FIFO queue)
            .Take(500)
            .ToListAsync(ct);
    }

    public Task<IdentityVerification?> GetByUserAsync(string userId, CancellationToken ct = default) =>
        _db.IdentityVerifications.AsNoTracking().FirstOrDefaultAsync(v => v.UserId == userId, ct);

    private async Task<ServiceResult> ReviewAsync(
        int verificationId, string adminId, bool approve, string? reason, CancellationToken ct)
    {
        // Defense in depth: even if a controller forgets [Authorize(Roles = "Admin")], this still holds.
        if (!await IsAdminAsync(adminId, ct))
        {
            _logger.LogWarning("Non-admin {UserId} attempted to review verification {Id}", adminId, verificationId);
            return ServiceResult.Fail("غير مصرح لك بتنفيذ هذا الإجراء.");
        }

        var verification = await _db.IdentityVerifications
            .Include(v => v.User)
            .FirstOrDefaultAsync(v => v.Id == verificationId, ct);

        if (verification is null)
            return ServiceResult.Fail("طلب التوثيق غير موجود.");
        if (verification.UserId == adminId)
            return ServiceResult.Fail("لا يمكنك مراجعة طلب التوثيق الخاص بك.");
        if (verification.Status != VerificationStatus.Pending)
            return ServiceResult.Fail("تمت مراجعة هذا الطلب مسبقاً.");
        if (approve && verification.NationalIdExpiryDate.Date <= DateTime.UtcNow.Date)
            return ServiceResult.Fail("لا يمكن الموافقة: بطاقة الهوية منتهية الصلاحية. ارفض الطلب بدلاً من ذلك.");

        var cleanReason = TextUtil.Clean(reason, 200);

        verification.Status = approve ? VerificationStatus.Approved : VerificationStatus.Rejected;
        verification.User.IsVerified = approve;

        var text = approve
            ? "تم توثيق هويتك بنجاح، يمكنك الآن إضافة العناصر وحجزها."
            : "تم رفض طلب توثيق الهوية." + (cleanReason.Length == 0 ? "" : $" السبب: {cleanReason}");

        _db.Notifications.Add(new Notification
        {
            UserId = verification.UserId,
            Message = TextUtil.Clean(text, 255)
        });

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict reviewing verification {Id}", verificationId);
            return ServiceResult.Fail("تمت مراجعة هذا الطلب من أدمن آخر، أعد تحميل الصفحة.");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to save review of verification {Id}", verificationId);
            return ServiceResult.Fail("تعذر تنفيذ العملية الآن، حاول مرة أخرى.");
        }

        // Audit trail: who decided what. Deliberately contains no national-id data.
        _logger.LogInformation("Verification {Id} {Decision} by admin {AdminId}",
            verificationId, approve ? "APPROVED" : "REJECTED", adminId);

        if (!string.IsNullOrWhiteSpace(verification.User.Email))
        {
            try
            {
                await _email.SendAsync(verification.User.Email,
                    approve ? "تم توثيق هويتك" : "تم رفض طلب التوثيق", text, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send verification e-mail to user {UserId}", verification.UserId);
            }
        }

        return ServiceResult.Ok();
    }

    // ───────────────────────── Helpers ─────────────────────────

    private Task<bool> IsAdminAsync(string? userId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userId)) return Task.FromResult(false);

        return (from ur in _db.UserRoles.AsNoTracking()
                join r in _db.Roles.AsNoTracking() on ur.RoleId equals r.Id
                where ur.UserId == userId && r.NormalizedName == AdminRoleNormalized
                select ur).AnyAsync(ct);
    }

    /// <summary>Egyptian national id: C YYMMDD GG NNNN D. C = 2 (1900s) or 3 (2000s), followed by a valid birth date.</summary>
    private static bool TryParseNationalId(string id, out DateTime birthDate)
    {
        birthDate = default;
        if (!NationalIdRegex().IsMatch(id)) return false;

        var century = id[0] switch { '2' => 1900, '3' => 2000, _ => 0 };
        if (century == 0) return false;

        static int Two(string s, int i) => (s[i] - '0') * 10 + (s[i + 1] - '0');

        try
        {
            birthDate = new DateTime(century + Two(id, 1), Two(id, 3), Two(id, 5));
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }

        return birthDate <= DateTime.UtcNow.Date;
    }

    /// <summary>Blocks path traversal and non-image files. The controller must still validate the real file content.</summary>
    private static bool IsSafeImagePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Length > 260) return false;
        if (Path.IsPathRooted(path) || path.Contains("..") || path.Contains(':')) return false;
        if (path.Any(char.IsControl) || path.IndexOfAny(Path.GetInvalidPathChars()) >= 0) return false;

        return AllowedImageExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
    }
}