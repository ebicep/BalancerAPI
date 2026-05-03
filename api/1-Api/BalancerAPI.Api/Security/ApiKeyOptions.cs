using Microsoft.Extensions.Options;

namespace BalancerAPI.Api.Security;

/// <summary>
/// Pepper rotation contract:
///   * Each <c>api_clients</c> row stores the <c>pepper_version</c> used when its <c>secret_hash</c> was computed.
///   * <see cref="Pepper"/> + <see cref="PepperVersion"/> are the values used for newly issued keys.
///   * <see cref="PreviousPeppers"/> holds the pepper for each prior version that still has live keys.
///   * To rotate: bump <see cref="PepperVersion"/>, set the new <see cref="Pepper"/>, move the old pepper into
///     <see cref="PreviousPeppers"/> under its version. Re-issue keys at your own pace; once no rows reference
///     a previous version, drop it from <see cref="PreviousPeppers"/>.
/// </summary>
public sealed class ApiKeyOptions
{
    public const string SectionName = "Authentication:ApiKey";

    /// <summary>Minimum pepper length enforced at startup. 32 chars is plenty for a server-side secret.</summary>
    public const int MinPepperLength = 32;

    public string Pepper { get; set; } = "";

    public int PepperVersion { get; set; } = 1;

    /// <summary>Map of historical pepper version → pepper value. Bind via <c>Authentication:ApiKey:PreviousPeppers:&lt;n&gt;</c>.</summary>
    public Dictionary<int, string> PreviousPeppers { get; set; } = new();

    public bool TryGetPepper(int version, out string pepper)
    {
        if (version == PepperVersion)
        {
            pepper = Pepper;
            return !string.IsNullOrEmpty(pepper);
        }

        if (PreviousPeppers.TryGetValue(version, out var prev) && !string.IsNullOrEmpty(prev))
        {
            pepper = prev;
            return true;
        }

        pepper = "";
        return false;
    }
}

internal sealed class ApiKeyOptionsValidator : IValidateOptions<ApiKeyOptions>
{
    public ValidateOptionsResult Validate(string? name, ApiKeyOptions options)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(options.Pepper))
        {
            errors.Add($"{ApiKeyOptions.SectionName}:Pepper is required.");
        }
        else if (options.Pepper.Length < ApiKeyOptions.MinPepperLength)
        {
            errors.Add($"{ApiKeyOptions.SectionName}:Pepper must be at least {ApiKeyOptions.MinPepperLength} characters.");
        }

        if (options.PepperVersion < 1)
        {
            errors.Add($"{ApiKeyOptions.SectionName}:PepperVersion must be >= 1.");
        }

        foreach (var (version, value) in options.PreviousPeppers)
        {
            if (version < 1)
            {
                errors.Add($"{ApiKeyOptions.SectionName}:PreviousPeppers key {version} must be >= 1.");
            }
            if (version == options.PepperVersion)
            {
                errors.Add(
                    $"{ApiKeyOptions.SectionName}:PreviousPeppers must not contain the current PepperVersion ({version}).");
            }
            if (string.IsNullOrEmpty(value) || value.Length < ApiKeyOptions.MinPepperLength)
            {
                errors.Add(
                    $"{ApiKeyOptions.SectionName}:PreviousPeppers[{version}] must be at least {ApiKeyOptions.MinPepperLength} characters.");
            }
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
