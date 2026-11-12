namespace SalefnyMVC.Services;

public interface IEmailService
{
    /// <param name="message">Plain text. The service HTML-encodes it, so callers never build HTML.</param>
    Task SendAsync(string to, string subject, string message, CancellationToken ct = default);
}
