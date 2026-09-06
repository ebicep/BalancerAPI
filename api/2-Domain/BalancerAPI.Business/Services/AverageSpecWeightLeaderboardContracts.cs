using System.Text.Json.Serialization;

namespace BalancerAPI.Business.Services;

public interface IAverageSpecWeightLeaderboardService
{
    Task<IReadOnlyList<AverageSpecWeightLeaderboardEntry>> GetLeaderboardAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}

public sealed class AverageSpecWeightLeaderboardEntry
{
    [JsonPropertyName("uuid")]
    public required string Uuid { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("average-weight")]
    public double AverageWeight { get; init; }
}
