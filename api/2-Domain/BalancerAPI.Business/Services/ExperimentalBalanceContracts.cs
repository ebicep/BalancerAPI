using System.Text.Json.Serialization;

namespace BalancerAPI.Business.Services;

public sealed record ExperimentalBalanceRequest(
    [property: JsonPropertyName("players")] IReadOnlyList<Guid> Players);

public sealed record ExperimentalBalanceResponse(
    [property: JsonPropertyName("balance")] IReadOnlyDictionary<string, string> Balance,
    [property: JsonPropertyName("meta")] ExperimentalBalanceMeta Meta);

public sealed record ExperimentalBalanceMeta(
    [property: JsonPropertyName("iterations")] int Iterations,
    [property: JsonPropertyName("elapsedMs")] double ElapsedMs,
    [property: JsonPropertyName("blueOff")] int BlueOff,
    [property: JsonPropertyName("redOff")] int RedOff,
    [property: JsonPropertyName("weightDiff")] int WeightDiff,
    [property: JsonPropertyName("flatTeamDiff")] int FlatTeamDiff,
    [property: JsonPropertyName("wlDiff")] int WlDiff,
    [property: JsonPropertyName("kdDiff")] int KdDiff,
    [property: JsonPropertyName("specTypeDiff")] int SpecTypeDiff);

public sealed record ExperimentalBalanceServiceResult(
    bool Success,
    ExperimentalBalanceResponse? Data,
    ExperimentalBalanceError? Error);

public sealed record ExperimentalBalanceError(
    int StatusCode,
    string Message,
    IReadOnlyList<Guid>? MissingUuids = null);
