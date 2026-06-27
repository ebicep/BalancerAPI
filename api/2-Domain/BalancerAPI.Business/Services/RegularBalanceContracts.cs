using System.Text.Json.Serialization;

namespace BalancerAPI.Business.Services;

public sealed record RegularBalanceRequest(
    [property: JsonPropertyName("players")] IReadOnlyList<Guid> Players);

public sealed record RegularBalancePlayer(
    [property: JsonPropertyName("uuid")] Guid Uuid,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("weight")] int Weight,
    [property: JsonPropertyName("talker")] int Talker,
    [property: JsonPropertyName("win_loss")] int WinLoss,
    [property: JsonPropertyName("net_kd_per_game")] double NetKdPerGame);

public sealed record RegularBalanceTeam(
    [property: JsonPropertyName("total_weight")] int TotalWeight,
    [property: JsonPropertyName("total_talkers")] int TotalTalkers,
    [property: JsonPropertyName("total_win_loss")] int TotalWinLoss,
    [property: JsonPropertyName("total_net_kd_per_game")] double TotalNetKdPerGame,
    [property: JsonPropertyName("players")] IReadOnlyList<RegularBalancePlayer> Players);

public sealed record RegularBalanceResponse(
    [property: JsonPropertyName("balance_id")] Guid BalanceId,
    [property: JsonPropertyName("balance")] IReadOnlyList<RegularBalanceTeam> Balance,
    [property: JsonPropertyName("meta")] ExperimentalBalanceMeta Meta);

public sealed record RegularBalanceServiceResult(
    bool Success,
    RegularBalanceResponse? Data,
    RegularBalanceError? Error);

public sealed record RegularBalanceError(
    int StatusCode,
    string Message,
    IReadOnlyList<Guid>? MissingUuids = null);
