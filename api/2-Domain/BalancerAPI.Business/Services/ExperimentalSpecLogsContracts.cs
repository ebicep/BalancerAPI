using System.Text.Json.Serialization;

namespace BalancerAPI.Business.Services;

public interface IExperimentalSpecLogsService
{
    Task<ExperimentalSpecLogsResult> GetAllAsync(CancellationToken cancellationToken);

    Task<ExperimentalSpecLogsResult> TruncateAsync(CancellationToken cancellationToken);

    Task<ExperimentalSpecLogsResult> TruncateLastAsync(CancellationToken cancellationToken);

    Task<ExperimentalSpecLogsResult> ClearAsync(CancellationToken cancellationToken);

    Task<ExperimentalSpecLogsResult> UntruncateAsync(
        ExperimentalSpecLogsResponse? request,
        CancellationToken cancellationToken);
}

public sealed record ExperimentalSpecLogRowSnapshot(
    Guid? Id,
    Guid? BalanceId,
    Guid? Pyromancer,
    Guid? Cryomancer,
    Guid? Aquamancer,
    Guid? Berserker,
    Guid? Defender,
    Guid? Revenant,
    Guid? Avenger,
    Guid? Crusader,
    Guid? Protector,
    Guid? Thunderlord,
    Guid? Spiritguard,
    Guid? Earthwarden,
    Guid? Assassin,
    Guid? Vindicator,
    Guid? Apothecary,
    Guid? Conjurer,
    Guid? Sentinel,
    Guid? Luminary);

public sealed record ExperimentalSpecLogsViewResponse(
    int Count,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Log);

public sealed record ExperimentalSpecLogsResponse(
    int Count,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Log,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<ExperimentalSpecLogRowSnapshot>? Rows = null);

public sealed record ExperimentalSpecLogsResult(
    bool Success,
    int StatusCode,
    string? Message,
    ExperimentalSpecLogsResponse? Data);
