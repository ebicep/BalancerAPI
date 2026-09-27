using System.Text.Json.Serialization;

namespace BalancerAPI.Business.Services;

public interface IWeeklyAdjustPatchService
{
    Task<WeeklyAdjustPatchResponse> ApplyPatchAsync(CancellationToken cancellationToken);
}

public sealed record WeeklyAdjustPatchResponse(
    [property: JsonPropertyName("count")] int Count,
    [property: JsonPropertyName("recordedAt")] DateTime RecordedAt,
    [property: JsonPropertyName("cutoff")] DateTime Cutoff,
    [property: JsonPropertyName("adjusted")] IReadOnlyList<WeeklyAdjustPatchEntry> Adjusted);

public sealed record WeeklyAdjustPatchEntry(
    [property: JsonPropertyName("uuid")] Guid Uuid,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("spec")] string Spec,
    [property: JsonPropertyName("autoSpecDelta")] int AutoSpecDelta,
    [property: JsonPropertyName("manualSpecDelta")] int ManualSpecDelta,
    [property: JsonPropertyName("netSpecDelta")] int NetSpecDelta,
    [property: JsonPropertyName("patchAdjusted")] int PatchAdjusted,
    [property: JsonPropertyName("executed")] bool Executed,
    [property: JsonPropertyName("previousOffset")] int PreviousOffset,
    [property: JsonPropertyName("currentOffset")] int CurrentOffset,
    [property: JsonPropertyName("previousWeight")] int PreviousWeight,
    [property: JsonPropertyName("currentWeight")] int CurrentWeight,
    [property: JsonPropertyName("weightChange")] int WeightChange,
    [property: JsonPropertyName("baseWeight")] int BaseWeight);
