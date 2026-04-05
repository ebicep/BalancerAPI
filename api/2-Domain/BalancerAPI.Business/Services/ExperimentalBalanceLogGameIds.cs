using System.Globalization;
using System.Text.RegularExpressions;

namespace BalancerAPI.Business.Services;

public static partial class ExperimentalBalanceLogGameIds
{
    /// <summary>Stored for balance logs when no Mongo game id is supplied (required for composite PK; balance API does not accept <c>game_id</c>).</summary>
    public const string Sentinel = "000000000000000000000000";

    [GeneratedRegex("^[0-9a-fA-F]{24}$", RegexOptions.CultureInvariant)]
    private static partial Regex ObjectIdHexRegex();

    /// <summary>Returns normalized lowercase hex, or null if invalid.</summary>
    public static string? TryNormalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var t = value.Trim();
        return ObjectIdHexRegex().IsMatch(t) ? t.ToLowerInvariant() : null;
    }
}
