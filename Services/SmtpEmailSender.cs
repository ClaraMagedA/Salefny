using Microsoft.Extensions.Options;
using SalefnyMVC.Models.Options;
using System.Net;
using System.Net.Mail;

namespace SalefnyMVC.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly EmailOptions _options;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string htmlBody)
        {
            if (!_options.IsConfigured)
            {
                _logger.LogWarning("Email OTP requested but SMTP is not configured in appsettings.json.");
                return false;
            }

            try
            {
                using var client = new SmtpClient(_options.Host, _options.Port)
                {
                    EnableSsl = _options.EnableSsl,
                    Credentials = new NetworkCredential(_options.User, _options.Password),
                    Timeout = 10000
                };

                using var message = new MailMessage
                {
                    From = new MailAddress(_options.FromAddress, _options.FromName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };
                message.To.Add(to);

                await client.SendMailAsync(message);
                _logger.LogInformation("OTP email sent to {Email}.", to);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP email to {Email}.", to);
                return false;
            }
        }
    }
}
