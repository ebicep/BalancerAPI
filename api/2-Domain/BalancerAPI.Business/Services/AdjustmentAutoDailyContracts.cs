using System.Text.Json.Serialization;

namespace BalancerAPI.Business.Services;

public interface IAdjustmentAutoDailyService
{
    Task<AdjustmentAutoDailyResponse> ApplyAutoDailyAsync(CancellationToken cancellationToken);

    Task<AdjustmentAutoDailyUndoResult> UndoAutoDailyAsync(
        AdjustmentAutoDailyResponse request,
        CancellationToken cancellationToken);
}

public sealed record AdjustmentAutoDailyUndoResult(
    bool Success,
    int StatusCode,
    string? Message,
    AdjustmentAutoDailyResponse? Response = null)
{
    public static AdjustmentAutoDailyUndoResult Ok(AdjustmentAutoDailyResponse response) =>
        new(true, 200, null, response);

    public static AdjustmentAutoDailyUndoResult Fail(int statusCode, string message) =>
        new(false, statusCode, message);
}

public sealed record AdjustmentAutoDailyResponse(
    [property: JsonPropertyName("count")] int Count,
    [property: JsonPropertyName("adjusted")] IReadOnlyList<AdjustmentAutoDailyAdjustedEntry> Adjusted,
    [property: JsonPropertyName("date")] DateTime? Date = null);

public sealed record AdjustmentAutoDailyAdjustedEntry(
    [property: JsonPropertyName("uuid")] Guid Uuid,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("previousWeight")] int PreviousWeight,
    [property: JsonPropertyName("currentWeight")] int CurrentWeight,
    [property: JsonPropertyName("previousTrajectory")] int PreviousTrajectory,
    [property: JsonPropertyName("newTrajectory")] int NewTrajectory);
