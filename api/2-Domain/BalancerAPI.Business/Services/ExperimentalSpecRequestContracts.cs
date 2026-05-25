using System.Text.Json.Serialization;

namespace BalancerAPI.Business.Services;

public sealed record ExperimentalSpecRequestBody(
    [property: JsonPropertyName("spec")] string Spec);

public sealed record ExperimentalSpecRequestResponse(
    [property: JsonPropertyName("uuid")] Guid Uuid,
    [property: JsonPropertyName("spec")] string Spec,
    [property: JsonPropertyName("game_cooldown")] int GameCooldown,
    [property: JsonPropertyName("created_time")] DateTime CreatedTime);

public sealed record ExperimentalSpecRequestServiceResult(
    bool Success,
    int StatusCode,
    string? Message,
    ExperimentalSpecRequestResponse? Data);
