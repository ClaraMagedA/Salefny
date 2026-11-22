namespace SalefnyMVC.Services
{
    public interface IEmailSender
    {
        // Returns true only when the message was actually handed to the SMTP server.
        Task<bool> SendEmailAsync(string to, string subject, string htmlBody);
    }
}
