namespace BalancerAPI.Business.Services;

public interface IExperimentalSpecLogsService
{
    Task<ExperimentalSpecLogsResult> GetAllAsync(CancellationToken cancellationToken);

    Task<ExperimentalSpecLogsResult> TruncateAsync(CancellationToken cancellationToken);

    Task<ExperimentalSpecLogsResult> ClearAsync(CancellationToken cancellationToken);
}

public sealed record ExperimentalSpecLogsResponse(
    int Count,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Log);

public sealed record ExperimentalSpecLogsResult(
    bool Success,
    int StatusCode,
    string? Message,
    ExperimentalSpecLogsResponse? Data);
