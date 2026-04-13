using System.Text.Json.Serialization;

namespace BalancerAPI.Business.Services;

public interface IExperimentalBalanceInputService
{
    Task<ExperimentalBalanceInputServiceResult> InputAsync(
        Guid balanceId,
        ExperimentalBalanceInputBody body,
        CancellationToken cancellationToken);

    Task<ExperimentalBalanceInputServiceResult> UninputAsync(Guid balanceId, CancellationToken cancellationToken);

    Task<ExperimentalBalanceInputServiceResult> ClearInputAsync(Guid balanceId, CancellationToken cancellationToken);
}

public sealed record ExperimentalBalanceInputServiceResult(
    bool Success,
    int StatusCode,
    string? Message);

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
    [property: JsonPropertyName("kills")] int Kills,
    [property: JsonPropertyName("deaths")] int Deaths);

public sealed record ExperimentalBalanceInputResponse(
    [property: JsonPropertyName("balance_id")] Guid BalanceId);
