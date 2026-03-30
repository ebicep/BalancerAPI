using System.Text.Json.Serialization;

namespace BalancerAPI.Business.Services;

public sealed record ExperimentalBalanceRequest(
    [property: JsonPropertyName("players")] IReadOnlyList<Guid> Players);

public sealed record ExperimentalBalanceResponse(
    [property: JsonPropertyName("balance")] IReadOnlyList<ExperimentalBalanceTeam> Balance,
    [property: JsonPropertyName("meta")] ExperimentalBalanceMeta Meta);

public sealed record ExperimentalBalanceTeam(
    [property: JsonPropertyName("total_weight")] int TotalWeight,
    [property: JsonPropertyName("total_talkers")] int TotalTalkers,
    [property: JsonPropertyName("total_win_loss")] int TotalWinLoss,
    [property: JsonPropertyName("total_net_kd_per_game")] double TotalNetKdPerGame,
    [property: JsonPropertyName("specs")] IReadOnlyList<ExperimentalBalancePlayerSpec> Specs);

public sealed record ExperimentalBalancePlayerSpec(
    [property: JsonPropertyName("uuid")] Guid Uuid,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("spec")] string Spec,
    [property: JsonPropertyName("weight")] int Weight,
    [property: JsonPropertyName("talker")] int Talker,
    [property: JsonPropertyName("win_loss")] int WinLoss,
    [property: JsonPropertyName("net_kd_per_game")] double NetKdPerGame);

public sealed record ExperimentalBalanceMeta(
    [property: JsonPropertyName("iterations")] int Iterations,
    [property: JsonPropertyName("durationMs")] double DurationMs,
    [property: JsonPropertyName("steps")] IReadOnlyList<ExperimentalBalanceMetaStep> Steps,
    [property: JsonPropertyName("season")] int Season,
    [property: JsonPropertyName("time")] DateTime Time);

public sealed record ExperimentalBalanceMetaStep(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("durationMs")] double DurationMs,
    [property: JsonPropertyName("startOffsetMs")] double StartOffsetMs);

public sealed record ExperimentalBalanceServiceResult(
    bool Success,
    ExperimentalBalanceResponse? Data,
    ExperimentalBalanceError? Error);

public sealed record ExperimentalBalanceError(
    int StatusCode,
    string Message,
    IReadOnlyList<Guid>? MissingUuids = null);
