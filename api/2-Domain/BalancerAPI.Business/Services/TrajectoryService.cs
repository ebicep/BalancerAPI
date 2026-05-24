using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Business.Services;

public sealed class TrajectoryService(
    BalancerDbContext dbContext,
    IPlayerKeyResolver playerKeyResolver) : ITrajectoryService
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
        var resolved = await playerKeyResolver.ResolveAsync(playerKey, cancellationToken);
        if (!resolved.Success || resolved.Uuid is null)
        {
            return TrajectoryServiceResult<PlayerTrajectoryEntry>.Fail(
                resolved.StatusCode,
                resolved.Message!);
        }

        var uuid = resolved.Uuid.Value;
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
}
