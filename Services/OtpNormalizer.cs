using System.Text;

namespace SalefnyMVC.Services
{
    public static class OtpNormalizer
    {
        // Accept Arabic-Indic (٠-٩) and Extended Arabic-Indic (۰-۹) digits as well as ASCII.
        public static string Normalize(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            var result = new StringBuilder();
            foreach (var c in input.Trim())
            {
                if (c >= '\u0660' && c <= '\u0669')
                    result.Append((char)('0' + (c - '\u0660')));
                else if (c >= '\u06F0' && c <= '\u06F9')
                    result.Append((char)('0' + (c - '\u06F0')));
                else
                    result.Append(c);
            }
            return result.ToString();
        }
    }
}
