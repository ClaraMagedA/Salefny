using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Options;

namespace SalefnyMVC.Services;

public sealed class EmailSettings
{
    public string Host { get; set; } = string.Empty;   // empty => test mode (log only, nothing is sent)
    public int Port { get; set; } = 587;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty; // never commit: use user-secrets / environment variables
    public string FromAddress { get; set; } = "no-reply@salefny.local";
    public string FromName { get; set; } = "سلفني";
}

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> options, ILogger<EmailService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string message, CancellationToken ct = default)
    {
        if (!MailAddress.TryCreate(to, out var recipient))
            throw new ArgumentException("Invalid recipient address.", nameof(to));

        // Stripping control characters blocks e-mail header injection (CR/LF in the subject).
        var safeSubject = TextUtil.Clean(subject, 150);

        if (string.IsNullOrWhiteSpace(_settings.Host))
        {
            // Test mode. The body and the full address are deliberately not logged (personal data).
            _logger.LogInformation("[TEST MAIL] To: {To} | Subject: {Subject}", Mask(recipient.Address), safeSubject);
            return;
        }

        using var mail = new MailMessage
        {
            From = new MailAddress(_settings.FromAddress, _settings.FromName),
            Subject = safeSubject,
            SubjectEncoding = Encoding.UTF8,
            Body = BuildHtml(message),
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = true
        };
        mail.To.Add(recipient);

        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = true,          // TLS is always required
            Timeout = 15_000,
            Credentials = string.IsNullOrEmpty(_settings.UserName)
                ? null
                : new NetworkCredential(_settings.UserName, _settings.Password)
        };

        await client.SendMailAsync(mail, ct);
    }

    // The message is encoded, so user-controlled text (item titles, rejection reasons)
    // can never inject HTML or links into an e-mail.
    private static string BuildHtml(string message)
    {
        var encoded = WebUtility.HtmlEncode(message ?? string.Empty).Replace("\n", "<br/>");
        return $"<div dir=\"rtl\" style=\"font-family:Tahoma,Arial,sans-serif;line-height:1.8\">{encoded}</div>";
    }

    private static string Mask(string address)
    {
        var at = address.IndexOf('@');
        return at <= 1 ? "***" : $"{address[0]}***{address[at..]}";
    }
}
