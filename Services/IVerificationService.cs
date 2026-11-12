using SalefnyMVC.Models.Entities;

namespace SalefnyMVC.Services;

public interface IVerificationService
{

    Task<ServiceResult> SubmitAsync(string userId, string nationalId, DateTime expiryDate,
        string frontIdImagePath, string selfieImagePath, CancellationToken ct = default);

    // adminId must come from the authenticated principal. The service re-checks the Admin role itself.
    Task<ServiceResult> ApproveAsync(int verificationId, string adminId, CancellationToken ct = default);
    Task<ServiceResult> RejectAsync(int verificationId, string adminId, string? reason, CancellationToken ct = default);

    /// <exception cref="UnauthorizedAccessException">Caller is not an admin.</exception>
    Task<IReadOnlyList<IdentityVerification>> GetPendingAsync(string adminId, CancellationToken ct = default);

    Task<IdentityVerification?> GetByUserAsync(string userId, CancellationToken ct = default);
}
