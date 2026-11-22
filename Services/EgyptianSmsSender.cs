using Microsoft.Extensions.Options;
using SalefnyMVC.Models.Options;
using System.Net;
using System.Text;

namespace SalefnyMVC.Services
{
    // Talks to an Egyptian SMS gateway using the common SMSMisr-style REST API:
    // {BaseUrl}?username=..&password=..&language=..&sender=..&mobile=..&message=..
    public class EgyptianSmsSender : ISmsSender
    {
        private readonly SmsGatewayOptions _options;
        private readonly HttpClient _http;
        private readonly ILogger<EgyptianSmsSender> _logger;

        public EgyptianSmsSender(
            IOptions<SmsGatewayOptions> options,
            HttpClient http,
            ILogger<EgyptianSmsSender> logger)
        {
            _options = options.Value;
            _http = http;
            _logger = logger;
        }

        public async Task<(bool Success, string? Error)> SendSmsAsync(string phoneNumber, string message)
        {
            if (!_options.IsConfigured)
                return (false, "بوابة الرسائل غير مُعدّة في appsettings.json.");

            var mobile = NormalizeMobile(phoneNumber);
            if (mobile is null)
                return (false, "صيغة رقم الهاتف غير صحيحة. استخدم صيغة 01xxxxxxxxx.");

            var query = new StringBuilder()
                .Append("username=").Append(WebUtility.UrlEncode(_options.Username))
                .Append("&password=").Append(WebUtility.UrlEncode(_options.Password))
                .Append("&language=").Append(WebUtility.UrlEncode(_options.Language))
                .Append("&sender=").Append(WebUtility.UrlEncode(_options.Sender))
                .Append("&mobile=").Append(WebUtility.UrlEncode(mobile))
                .Append("&message=").Append(WebUtility.UrlEncode(message))
                .ToString();

            try
            {
                var url = _options.BaseUrl.TrimEnd('?', '&') +
                          (_options.BaseUrl.Contains('?') ? "&" : "?") + query;

                var response = await _http.GetAsync(url);
                var body = (await response.Content.ReadAsStringAsync()).Trim();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("SMS gateway returned {Status}: {Body}", response.StatusCode, body);
                    return (false, "فشل إرسال الرسالة من بوابة الرسائل.");
                }

                if (body.Contains("success", StringComparison.OrdinalIgnoreCase) ||
                    body.Contains("\"code\":\"0\"") ||
                    body.Contains("\"status\":1") ||
                    body.StartsWith("1", StringComparison.Ordinal))
                {
                    _logger.LogInformation("Verification SMS sent to {Phone}.", mobile);
                    return (true, null);
                }

                _logger.LogWarning("SMS gateway responded without success for {Phone}: {Body}", mobile, body);
                return (false, "لم تأكد بوابة الرسائل من إرسال الرسالة. حاول مرة أخرى.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMS gateway request failed for {Phone}.", mobile);
                return (false, "تعذر الاتصال ببوابة الرسائل.");
            }
        }

        private static string? NormalizeMobile(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return null;
            var digits = new string(phone.Where(char.IsDigit).ToArray());
            if (digits.Length == 11 && digits.StartsWith("01")) return digits;
            if (digits.Length == 12 && digits.StartsWith("201")) return digits;
            if (digits.Length == 10 && digits.StartsWith("1")) return "0" + digits;
            return null;
        }
    }
}
