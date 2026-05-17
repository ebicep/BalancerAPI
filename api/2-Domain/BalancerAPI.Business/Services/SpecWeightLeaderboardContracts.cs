using System.Text.Json.Serialization;

namespace BalancerAPI.Business.Services;

public interface ISpecWeightLeaderboardService
{
    Task<Dictionary<string, IReadOnlyList<SpecWeightLeaderboardEntry>>> GetLeaderboardAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}

public sealed class SpecWeightLeaderboardEntry
{
    [JsonPropertyName("uuid")]
    public required string Uuid { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("spec-weight")]
    public int SpecWeight { get; init; }
}
