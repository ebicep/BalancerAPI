using BalancerAPI.Data.Data;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Business.Services;

public interface IPlayerKeyResolver
{
    Task<PlayerKeyResolveResult> ResolveAsync(string playerKey, CancellationToken cancellationToken);
}

public sealed record PlayerKeyResolveResult(
    bool Success,
    int StatusCode,
    string? Message,
    Guid? Uuid,
    string? DisplayName);

public sealed class PlayerKeyResolver(BalancerDbContext dbContext) : IPlayerKeyResolver
{
    public async Task<PlayerKeyResolveResult> ResolveAsync(string playerKey, CancellationToken cancellationToken)
    {
        var trimmed = playerKey.Trim();
        if (trimmed.Length == 0)
        {
            return new PlayerKeyResolveResult(false, 400, "Player identifier is required.", null, null);
        }

        if (Guid.TryParse(trimmed, out var uuid))
        {
            var namesByUuid = await dbContext.Names.AsNoTracking()
                .Where(x => x.Uuid == uuid)
                .Select(x => x.Name)
                .Distinct()
                .ToListAsync(cancellationToken);
            if (namesByUuid.Count > 1)
            {
                return new PlayerKeyResolveResult(
                    false,
                    409,
                    $"Player UUID has multiple names in names table: {uuid}.",
                    null,
                    null);
            }

            var displayName = namesByUuid.Count == 1 ? namesByUuid[0] : string.Empty;
            return new PlayerKeyResolveResult(true, 200, null, uuid, displayName);
        }

        var normalizedName = trimmed.ToLowerInvariant();
        var rows = await dbContext.Names.AsNoTracking()
            .Where(x => x.Name.ToLower() == normalizedName)
            .Select(x => new { x.Uuid, x.Name })
            .Distinct()
            .ToListAsync(cancellationToken);

        return rows.Count switch
        {
            > 1 => new PlayerKeyResolveResult(
                false,
                409,
                $"Player name is ambiguous in names table: {trimmed}.",
                null,
                null),
            0 => new PlayerKeyResolveResult(
                false,
                404,
                $"No matching UUID found in names table for: {trimmed}.",
                null,
                null),
            _ => new PlayerKeyResolveResult(true, 200, null, rows[0].Uuid, rows[0].Name)
        };
    }
}
