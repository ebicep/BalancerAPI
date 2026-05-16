using System.Text.Json.Serialization;

namespace BalancerAPI.Business.Services;

public interface IExperimentalBalanceInputService
{
    Task<ExperimentalBalanceInputServiceResult> InputAsync(
        Guid balanceId,
        ExperimentalBalanceInputBody body,
        CancellationToken cancellationToken);

    Task<ExperimentalBalanceInputServiceResult> UninputAsync(
        Guid balanceId,
        ExperimentalBalanceInputBody body,
        CancellationToken cancellationToken);

    Task<ExperimentalBalanceInputServiceResult> ClearInputAsync(Guid balanceId, CancellationToken cancellationToken);
}

public sealed record ExperimentalBalanceInputServiceResult(
    bool Success,
    int StatusCode,
    string? Message,
    ExperimentalBalanceInputResponse? Response = null);

/// <summary>
/// Serialized to <c>experimental_balance_log.input</c> on first successful input; must match stored JSON when re-applying after uninput.
/// <paramref name="GameId"/> is written to <c>experimental_balance_log.game_id</c> on success.
/// </summary>
public sealed record ExperimentalBalanceInputBody(
    [property: JsonPropertyName("winners")] IReadOnlyList<ExperimentalBalanceInputPlayerLine>? Winners,
    [property: JsonPropertyName("losers")] IReadOnlyList<ExperimentalBalanceInputPlayerLine>? Losers,
    [property: JsonPropertyName("game_id")] string? GameId);

public sealed record ExperimentalBalanceInputPlayerLine(
    [property: JsonPropertyName("uuid")] Guid Uuid,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("kills")] int Kills,
    [property: JsonPropertyName("deaths")] int Deaths);

/// <summary>
/// One player's before/after snapshot for the input or uninput response.
/// On successful <c>input</c> or <c>uninput</c>, trajectory and daily W/L/K/D reflect state before and after applying that operation.
/// </summary>
public sealed record ExperimentalBalanceChangeItem(
    [property: JsonPropertyName("uuid")] Guid Uuid,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("old_trajectory")] int? OldTrajectory,
    [property: JsonPropertyName("new_trajectory")] int? NewTrajectory,
    [property: JsonPropertyName("old_wins")] int OldWins,
    [property: JsonPropertyName("new_wins")] int NewWins,
    [property: JsonPropertyName("old_losses")] int OldLosses,
    [property: JsonPropertyName("new_losses")] int NewLosses,
    [property: JsonPropertyName("old_kills")] int OldKills,
    [property: JsonPropertyName("new_kills")] int NewKills,
    [property: JsonPropertyName("old_deaths")] int OldDeaths,
    [property: JsonPropertyName("new_deaths")] int NewDeaths);

/// <summary>
/// Returned from successful <c>input</c> or <c>uninput</c> with per-player <see cref="Changes"/>.
/// </summary>
public sealed record ExperimentalBalanceInputResponse(
    [property: JsonPropertyName("balance_id")] Guid BalanceId,
    [property: JsonPropertyName("changes")] IReadOnlyList<ExperimentalBalanceChangeItem>? Changes);
