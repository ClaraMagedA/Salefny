namespace SalefnyMVC.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            // SMTP or Test Mail implementation placeholder
            _logger.LogInformation($"[Email Simulated] To: {toEmail} | Subject: {subject} | Message: {message}");
            await Task.CompletedTask;
        }
    }
}