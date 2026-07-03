using System.Text.Json.Serialization;

namespace BalancerAPI.Business.Services;

public interface IBaseWeightLeaderboardService
{
    Task<IReadOnlyList<BaseWeightLeaderboardEntry>> GetLeaderboardAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}

public sealed class BaseWeightLeaderboardEntry
{
    [JsonPropertyName("uuid")]
    public required string Uuid { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("base-weight")]
    public int BaseWeight { get; init; }
}
