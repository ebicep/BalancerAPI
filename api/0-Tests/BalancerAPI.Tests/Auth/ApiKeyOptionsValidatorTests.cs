using BalancerAPI.Api.Security;

namespace BalancerAPI.Tests.Auth;

public sealed class ApiKeyOptionsValidatorTests
{
    private static readonly string ValidPepper = new('a', ApiKeyOptions.MinPepperLength);

    [Fact]
    public void Validate_ValidConfig_Succeeds()
    {
        var validator = new ApiKeyOptionsValidator();
        var result = validator.Validate(name: null, new ApiKeyOptions
        {
            Pepper = ValidPepper,
            PepperVersion = 1
        });
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_EmptyPepper_Fails()
    {
        var validator = new ApiKeyOptionsValidator();
        var result = validator.Validate(name: null, new ApiKeyOptions { Pepper = "", PepperVersion = 1 });
        Assert.True(result.Failed);
    }

    [Fact]
    public void Validate_PepperShorterThanMinimum_Fails()
    {
        var validator = new ApiKeyOptionsValidator();
        var result = validator.Validate(name: null, new ApiKeyOptions
        {
            Pepper = new string('a', ApiKeyOptions.MinPepperLength - 1),
            PepperVersion = 1
        });
        Assert.True(result.Failed);
    }

    [Fact]
    public void Validate_PreviousPepperShareCurrentVersion_Fails()
    {
        var validator = new ApiKeyOptionsValidator();
        var result = validator.Validate(name: null, new ApiKeyOptions
        {
            Pepper = ValidPepper,
            PepperVersion = 2,
            PreviousPeppers = new Dictionary<int, string> { [2] = ValidPepper }
        });
        Assert.True(result.Failed);
    }

    [Fact]
    public void TryGetPepper_CurrentVersion_ReturnsCurrentPepper()
    {
        var opts = new ApiKeyOptions { Pepper = ValidPepper, PepperVersion = 3 };
        Assert.True(opts.TryGetPepper(3, out var pepper));
        Assert.Equal(ValidPepper, pepper);
    }

    [Fact]
    public void TryGetPepper_PreviousVersion_ReturnsPreviousPepper()
    {
        var prev = new string('p', ApiKeyOptions.MinPepperLength);
        var opts = new ApiKeyOptions
        {
            Pepper = ValidPepper,
            PepperVersion = 2,
            PreviousPeppers = new Dictionary<int, string> { [1] = prev }
        };
        Assert.True(opts.TryGetPepper(1, out var pepper));
        Assert.Equal(prev, pepper);
    }

    [Fact]
    public void TryGetPepper_UnknownVersion_ReturnsFalse()
    {
        var opts = new ApiKeyOptions { Pepper = ValidPepper, PepperVersion = 1 };
        Assert.False(opts.TryGetPepper(99, out _));
    }
}
