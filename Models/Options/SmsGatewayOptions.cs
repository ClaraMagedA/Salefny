namespace SalefnyMVC.Models.Options
{
    public class SmsGatewayOptions
    {
        // e.g. https://www.smsmisr.com/api/send
        public string BaseUrl { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Sender { get; set; } = "Salefny";
        public string Language { get; set; } = "3";

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(BaseUrl) &&
            !string.IsNullOrWhiteSpace(Username) &&
            !string.IsNullOrWhiteSpace(Password);
    }
}
