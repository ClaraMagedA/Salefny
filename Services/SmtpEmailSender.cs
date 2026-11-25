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
                var cleanPassword = _options.Password.Replace(" ", "").Trim();
                var cleanUser = _options.User.Trim();
                var cleanFrom = _options.FromAddress.Trim();

                using var client = new SmtpClient(_options.Host.Trim(), _options.Port)
                {
                    EnableSsl = _options.EnableSsl,
                    UseDefaultCredentials = false,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Credentials = new NetworkCredential(cleanUser, cleanPassword),
                    Timeout = 15000
                };

                using var message = new MailMessage
                {
                    From = new MailAddress(cleanFrom, _options.FromName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };
                message.To.Add(to.Trim());

                await client.SendMailAsync(message);
                _logger.LogInformation("OTP email sent to {Email}.", to);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("=== SMTP ERROR ===");
                Console.WriteLine(ex.ToString());
                _logger.LogError(ex, "Failed to send OTP email to {Email}.", to);
                return false;
            }
        }
    }
}