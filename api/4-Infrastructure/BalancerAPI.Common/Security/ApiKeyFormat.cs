using System.Security.Cryptography;

namespace BalancerAPI.Common.Security;

public static class ApiKeyFormat
{
    private const string Prefix = "bkr_";

    /// <summary>
    /// base64url(32 bytes) without padding = 43 chars. Anything shorter cannot be a key we issued.
    /// </summary>
    public const int MinSecretLength = 43;

    public static bool TryParse(string? fullKey, out Guid publicId, out string secret)
    {
        publicId = Guid.Empty;
        secret = null!;

        if (string.IsNullOrEmpty(fullKey) || !fullKey.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var rest = fullKey.AsSpan(Prefix.Length);
        if (rest.Length < 36 + 1 + MinSecretLength || rest[36] != '_')
        {
            return false;
        }

        if (!Guid.TryParse(rest[..36], out publicId))
        {
            return false;
        }

        secret = rest[37..].ToString();
        return secret.Length >= MinSecretLength;
    }

    public static string BuildKey(Guid publicId, string secret) => $"{Prefix}{publicId:D}_{secret}";

    public static (string FullKey, Guid PublicId) GenerateNewKey()
    {
        var publicId = Guid.NewGuid();
        var secretBytes = RandomNumberGenerator.GetBytes(32);
        var secret = Convert.ToBase64String(secretBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        return ($"{Prefix}{publicId:D}_{secret}", publicId);
    }
}
