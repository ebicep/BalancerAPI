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

public sealed record ExperimentalSpecRequestUnrunPlayerResponse(
    [property: JsonPropertyName("uuid")] Guid Uuid,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("spec")] string Spec,
    [property: JsonPropertyName("game_cooldown")] int GameCooldown,
    [property: JsonPropertyName("created_time")] DateTime CreatedTime);

public sealed record ExperimentalSpecRequestUnrunResponse(
    [property: JsonPropertyName("restored")] IReadOnlyList<ExperimentalSpecRequestUnrunPlayerResponse> Restored,
    [property: JsonPropertyName("cooldown_increased")] IReadOnlyList<ExperimentalSpecRequestUnrunPlayerResponse> CooldownIncreased);

public sealed record ExperimentalSpecRequestUnrunServiceResult(
    bool Success,
    int StatusCode,
    string? Message,
    ExperimentalSpecRequestUnrunResponse? Data);
