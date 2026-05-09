using System.Net.Http.Json;

namespace BalancerAPI.Business.Services;

public interface IMinecraftPlayerResolveService
{
    Task<PlayerResolveResult> ResolveAsync(string player, CancellationToken cancellationToken);
}

public sealed class MinecraftPlayerResolveService(HttpClient httpClient) : IMinecraftPlayerResolveService
{
    public async Task<PlayerResolveResult> ResolveAsync(string player, CancellationToken cancellationToken)
    {
        var trimmed = player.Trim();
        if (trimmed.Length == 0)
        {
            return PlayerResolveResult.Fail(400, "Player is required.");
        }

        if (!Guid.TryParse(trimmed, out var parsedUuid))
        {
            return PlayerResolveResult.Fail(400, "UUID format is invalid.");
        }

        var profile = await FetchSessionProfileAsync(parsedUuid.ToString("N"), cancellationToken);
        return profile switch
        {
            null => PlayerResolveResult.Fail(404, "UUID was not found in Mojang session profile."),
            _ => PlayerResolveResult.Ok(parsedUuid, profile.Name)
        };

    }

    private async Task<MojangSessionProfileResponse?> FetchSessionProfileAsync(
        string uuidNoDash,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            $"https://sessionserver.mojang.com/session/minecraft/profile/{uuidNoDash}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<MojangSessionProfileResponse>(cancellationToken);
    }

}

public sealed record PlayerResolveResult(bool Success, int StatusCode, string? Message, Guid Uuid, string Name)
{
    public static PlayerResolveResult Ok(Guid uuid, string? name) =>
        new(true, 200, null, uuid, name ?? string.Empty);

    public static PlayerResolveResult Fail(int statusCode, string message) =>
        new(false, statusCode, message, Guid.Empty, string.Empty);
}

internal sealed record MojangSessionProfileResponse(string? Id, string? Name);
