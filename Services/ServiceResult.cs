using System.Text;

namespace SalefnyMVC.Services;

public class ServiceResult
{
    public bool Succeeded { get; protected init; }
    public string? Error { get; protected init; }

    public static ServiceResult Ok() => new() { Succeeded = true };
    public static ServiceResult Fail(string error) => new() { Succeeded = false, Error = error };
}

public sealed class ServiceResult<T> : ServiceResult
{
    public T? Data { get; private init; }

    public static ServiceResult<T> Ok(T data) => new() { Succeeded = true, Data = data };
    public new static ServiceResult<T> Fail(string error) => new() { Succeeded = false, Error = error };
}

internal static class TextUtil
{
  
    public static string Clean(string? text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var sb = new StringBuilder(text.Length);
        foreach (var ch in text)
            if (!char.IsControl(ch)) sb.Append(ch);

        var cleaned = sb.ToString().Trim();
        return cleaned.Length <= maxLength ? cleaned : cleaned[..maxLength];
    }
}
