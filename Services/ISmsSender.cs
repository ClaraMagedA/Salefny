namespace SalefnyMVC.Services
{
    public interface ISmsSender
    {
        Task<(bool Success, string? Error)> SendSmsAsync(string phoneNumber, string message);
    }
}
