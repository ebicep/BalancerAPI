using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Routing;

namespace BalancerAPI.Api.Routing;

public sealed partial class KebabCaseParameterTransformer : IOutboundParameterTransformer
{
    public string? TransformOutbound(object? value)
    {
        if (value is null)
        {
            return null;
        }

        var text = value.ToString();
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        // "ExperimentalSpecWeights" -> "experimental-spec-weights"
        // "IOThing" -> "io-thing"
        return KebabCaseWordBoundaryRegex().Replace(text, "$1-$2").ToLowerInvariant();
    }

    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex KebabCaseWordBoundaryRegex();
}

