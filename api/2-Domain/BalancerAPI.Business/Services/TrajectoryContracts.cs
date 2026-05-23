using System.Text.Json.Serialization;

namespace BalancerAPI.Business.Services;

public interface ITrajectoryService
{
    Task<IReadOnlyList<PlayerTrajectoryEntry>> ListAsync(CancellationToken cancellationToken);

    Task<TrajectoryServiceResult<PlayerTrajectoryEntry>> SetAsync(
        string playerKey,
        SetTrajectoryRequest body,
        CancellationToken cancellationToken);
}

public sealed record PlayerTrajectoryEntry(
    [property: JsonPropertyName("uuid")] Guid Uuid,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("trajectory")] int Trajectory);

public sealed record SetTrajectoryRequest(
    [property: JsonPropertyName("trajectory")] int Trajectory);

public sealed record TrajectoryServiceResult<T>(
    bool Success,
    int StatusCode,
    string? Message,
    T? Response)
{
    public static TrajectoryServiceResult<T> Ok(T response) =>
        new(true, 200, null, response);

    public static TrajectoryServiceResult<T> Fail(int statusCode, string message) =>
        new(false, statusCode, message, default);
}
