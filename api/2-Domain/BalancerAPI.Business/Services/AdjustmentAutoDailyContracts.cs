using System.Text.Json.Serialization;

namespace BalancerAPI.Business.Services;

public interface IAdjustmentAutoDailyService
{
    Task<AdjustmentAutoDailyResponse> ApplyAutoDailyAsync(CancellationToken cancellationToken);
}

public sealed record AdjustmentAutoDailyResponse(
    [property: JsonPropertyName("count")] int Count,
    [property: JsonPropertyName("adjusted")] IReadOnlyList<AdjustmentAutoDailyAdjustedEntry> Adjusted);

public sealed record AdjustmentAutoDailyAdjustedEntry(
    [property: JsonPropertyName("uuid")] Guid Uuid,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("previousWeight")] int PreviousWeight,
    [property: JsonPropertyName("currentWeight")] int CurrentWeight);
