using SalefnyMVC.Models.Entities;

namespace SalefnyMVC.Services
{
    public interface IVerificationService
    {
        Task<bool> ApproveVerificationAsync(int verificationId);
        Task<bool> RejectVerificationAsync(int verificationId);
    }
}