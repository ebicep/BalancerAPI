using System.Text.Json.Serialization;

namespace BalancerAPI.Business.Services;

public interface IAdjustmentAutoWeeklyService
{
    Task<AdjustmentAutoWeeklyResponse> ApplyAutoWeeklyAsync(CancellationToken cancellationToken);
}

public sealed record AdjustmentAutoWeeklyResponse(
    [property: JsonPropertyName("count")] int Count,
    [property: JsonPropertyName("adjusted")] Dictionary<Guid, AdjustmentAutoWeeklyPlayerBlock> Adjusted);

public sealed record AdjustmentAutoWeeklyPlayerBlock(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("baseWeight")] int BaseWeight,
    [property: JsonPropertyName("specs")] List<AdjustmentAutoWeeklySpecChange> Specs);

public sealed record AdjustmentAutoWeeklySpecChange(
    [property: JsonPropertyName("spec")] string Spec,
    [property: JsonPropertyName("previousWeight")] int PreviousWeight,
    [property: JsonPropertyName("currentWeight")] int CurrentWeight,
    [property: JsonPropertyName("previousOffset")] int PreviousOffset,
    [property: JsonPropertyName("currentOffset")] int CurrentOffset);
