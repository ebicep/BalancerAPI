using System.Text.Json.Serialization;

namespace BalancerAPI.Business.Services;

public interface IManualWeightAdjustmentService
{
    Task<ManualWeightAdjustServiceResult<ManualBaseAdjustResponse>> PatchBaseAsync(
        string playerKey,
        ManualAdjustBaseRequest body,
        CancellationToken cancellationToken);

    Task<ManualWeightAdjustServiceResult<ManualSpecAdjustResponse>> PatchSpecAsync(
        string playerKey,
        ManualAdjustSpecRequest body,
        CancellationToken cancellationToken);
}

public sealed record ManualAdjustBaseRequest(
    [property: JsonPropertyName("amount")] int Amount);

public sealed record ManualAdjustSpecRequest(
    [property: JsonPropertyName("amount")] int Amount,
    [property: JsonPropertyName("spec")] string? Spec);

public sealed record ManualBaseAdjustResponse(
    [property: JsonPropertyName("uuid")] Guid Uuid,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("previousWeight")] int PreviousWeight,
    [property: JsonPropertyName("newWeight")] int NewWeight);

public sealed record ManualSpecAdjustResponse(
    [property: JsonPropertyName("uuid")] Guid Uuid,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("spec")] string Spec,
    [property: JsonPropertyName("previousOffset")] int PreviousOffset,
    [property: JsonPropertyName("newOffset")] int NewOffset,
    [property: JsonPropertyName("baseWeight")] int BaseWeight,
    [property: JsonPropertyName("previousSpecWeight")] int PreviousSpecWeight,
    [property: JsonPropertyName("newSpecWeight")] int NewSpecWeight);

public sealed record ManualWeightAdjustServiceResult<T>(
    bool Success,
    int StatusCode,
    string? Message,
    T? Response)
{
    public static ManualWeightAdjustServiceResult<T> Ok(T response) =>
        new(true, 200, null, response);

    public static ManualWeightAdjustServiceResult<T> Fail(int statusCode, string message) =>
        new(false, statusCode, message, default);
}
