using System.Text.Json.Serialization;

namespace BalancerAPI.Business.Services;

public sealed record ExperimentalSpecBanRequest(
    [property: JsonPropertyName("spec")] string? Spec = null,
    [property: JsonPropertyName("class")] string? Class = null,
    [property: JsonPropertyName("specType")] string? SpecType = null);

public sealed record ExperimentalSpecBansResponse(
    [property: JsonPropertyName("bans")] IReadOnlyList<string> Bans);

public sealed record ExperimentalSpecBanServiceResult(
    bool Success,
    int StatusCode,
    string? Message,
    ExperimentalSpecBansResponse? Data);
