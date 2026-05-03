using System.Security.Cryptography;
using System.Text;

namespace BalancerAPI.Common.Security;

public static class ApiKeyHasher
{
    public static byte[] HashSecret(string secret, string pepper)
    {
        var input = Encoding.UTF8.GetBytes(secret + pepper);
        return SHA256.HashData(input);
    }

    public static bool FixedTimeEquals(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        return CryptographicOperations.FixedTimeEquals(left, right);
    }
}
