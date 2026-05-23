using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Business.Services;

public sealed class TrajectoryService(BalancerDbContext dbContext) : ITrajectoryService
{
    public async Task<IReadOnlyList<PlayerTrajectoryEntry>> ListAsync(CancellationToken cancellationToken)
    {
        var adjustments = await dbContext.AdjustmentDaily.AsNoTracking()
            .ToListAsync(cancellationToken);
        if (adjustments.Count == 0)
        {
            return [];
        }

        var uuids = adjustments.Select(a => a.Uuid).ToList();
        var names = await dbContext.Names.AsNoTracking()
            .Where(n => uuids.Contains(n.Uuid))
            .ToListAsync(cancellationToken);
        var displayNameByUuid = names
            .GroupBy(n => n.Uuid)
            .ToDictionary(g => g.Key, g => g.Min(n => n.Name)!);

        return adjustments
            .OrderByDescending(a => a.Trajectory)
            .ThenBy(a => displayNameByUuid.GetValueOrDefault(a.Uuid, string.Empty))
            .ThenBy(a => a.Uuid)
            .Select(a => new PlayerTrajectoryEntry(
                a.Uuid,
                displayNameByUuid.GetValueOrDefault(a.Uuid, string.Empty),
                a.Trajectory))
            .ToList();
    }

    public async Task<TrajectoryServiceResult<PlayerTrajectoryEntry>> SetAsync(
        string playerKey,
        SetTrajectoryRequest body,
        CancellationToken cancellationToken)
    {
        var resolved = await ResolvePlayerKeyAsync(playerKey, cancellationToken);
        if (!resolved.Success)
        {
            return TrajectoryServiceResult<PlayerTrajectoryEntry>.Fail(
                resolved.StatusCode,
                resolved.Message!);
        }

        var uuid = resolved.Uuid!.Value;
        var displayName = resolved.DisplayName ?? string.Empty;
        var hasBaseWeight = await dbContext.BaseWeights.AsNoTracking()
            .AnyAsync(x => x.Uuid == uuid, cancellationToken);
        if (!hasBaseWeight)
        {
            return TrajectoryServiceResult<PlayerTrajectoryEntry>.Fail(
                404,
                "Base weight row not found for player.");
        }

        var adjustmentDaily = await dbContext.AdjustmentDaily
            .AsTracking()
            .FirstOrDefaultAsync(x => x.Uuid == uuid, cancellationToken);
        if (adjustmentDaily is null)
        {
            adjustmentDaily = new AdjustmentDaily { Uuid = uuid, Trajectory = body.Trajectory };
            dbContext.AdjustmentDaily.Add(adjustmentDaily);
        }
        else
        {
            adjustmentDaily.Trajectory = body.Trajectory;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return TrajectoryServiceResult<PlayerTrajectoryEntry>.Ok(
            new PlayerTrajectoryEntry(uuid, displayName, adjustmentDaily.Trajectory));
    }

    private async Task<(bool Success, int StatusCode, string? Message, Guid? Uuid, string? DisplayName)> ResolvePlayerKeyAsync(
        string playerKey,
        CancellationToken cancellationToken)
    {
        var trimmed = playerKey.Trim();
        if (trimmed.Length == 0)
        {
            return (false, 400, "Player identifier is required.", null, null);
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
                return (false, 409, $"Player UUID has multiple names in names table: {uuid}.", null, null);
            }

            var displayName = namesByUuid.Count == 1 ? namesByUuid[0] : string.Empty;
            return (true, 200, null, uuid, displayName);
        }

        var normalizedName = trimmed.ToLowerInvariant();
        var rows = await dbContext.Names.AsNoTracking()
            .Where(x => x.Name.ToLower() == normalizedName)
            .Select(x => new { x.Uuid, x.Name })
            .Distinct()
            .ToListAsync(cancellationToken);

        return rows.Count switch
        {
            > 1 => (false, 409, $"Player name is ambiguous in names table: {trimmed}.", null, null),
            0 => (false, 404, $"No matching UUID found in names table for: {trimmed}.", null, null),
            _ => (true, 200, null, rows[0].Uuid, rows[0].Name)
        };
    }
}
