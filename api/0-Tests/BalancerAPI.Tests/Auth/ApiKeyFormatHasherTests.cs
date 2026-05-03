using BalancerAPI.Common.Security;

namespace BalancerAPI.Tests.Auth;

public sealed class ApiKeyFormatHasherTests
{
    [Fact]
    public void TryParse_BuildKey_RoundTrips()
    {
        var id = Guid.Parse("11223344-5566-7788-99aa-bbccddeeff00");
        const string secret = "abcXYZ-_123abcXYZ-_123abcXYZ-_123abcXYZ-_12";
        Assert.Equal(ApiKeyFormat.MinSecretLength, secret.Length);
        var key = ApiKeyFormat.BuildKey(id, secret);

        Assert.True(ApiKeyFormat.TryParse(key, out var pid, out var sec));
        Assert.Equal(id, pid);
        Assert.Equal(secret, sec);
    }

    [Fact]
    public void TryParse_RejectsSecretShorterThanMinimum()
    {
        var id = Guid.Parse("11223344-5566-7788-99aa-bbccddeeff00");
        var shortSecret = new string('a', ApiKeyFormat.MinSecretLength - 1);
        var key = ApiKeyFormat.BuildKey(id, shortSecret);

        Assert.False(ApiKeyFormat.TryParse(key, out _, out _));
    }

    [Fact]
    public void GenerateNewKey_ProducesParseableKey()
    {
        var (full, publicId) = ApiKeyFormat.GenerateNewKey();
        Assert.True(ApiKeyFormat.TryParse(full, out var pid, out var sec));
        Assert.Equal(publicId, pid);
        Assert.True(sec.Length >= ApiKeyFormat.MinSecretLength);
    }

    [Fact]
    public void HashSecret_IsStableAndVerifiableWithFixedTimeEquals()
    {
        var h = ApiKeyHasher.HashSecret("s", "pepper");
        Assert.Equal(32, h.Length);
        Assert.True(ApiKeyHasher.FixedTimeEquals(h, ApiKeyHasher.HashSecret("s", "pepper")));
        Assert.False(ApiKeyHasher.FixedTimeEquals(h, ApiKeyHasher.HashSecret("t", "pepper")));
    }
}
