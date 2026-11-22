using Microsoft.EntityFrameworkCore;
using SalefnyMVC.Data;
using SalefnyMVC.Models.Enums;

namespace SalefnyMVC.Services
{
    public class VerificationService : IVerificationService
    {
        private readonly SalefnyDbContext _context;

        public VerificationService(SalefnyDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ApproveVerificationAsync(int verificationId)
        {
            var verification = await _context.IdentityVerifications
                .Include(v => v.User)
                .FirstOrDefaultAsync(v => v.Id == verificationId);

            if (verification == null) return false;

            verification.Status = VerificationStatus.Approved;
            if (verification.User != null)
            {
                verification.User.IsVerified = true;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectVerificationAsync(int verificationId)
        {
            var verification = await _context.IdentityVerifications.FindAsync(verificationId);
            if (verification == null) return false;

            verification.Status = VerificationStatus.Rejected;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}