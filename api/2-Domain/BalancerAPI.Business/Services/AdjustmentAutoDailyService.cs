using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Business.Services;

public sealed class AdjustmentAutoDailyService(BalancerDbContext dbContext) : IAdjustmentAutoDailyService
{
    public async Task<AdjustmentAutoDailyResponse> ApplyAutoDailyAsync(CancellationToken cancellationToken)
    {
        var rows = await (
            from adj in dbContext.AdjustmentDaily
            where adj.Trajectory >= 3 || adj.Trajectory <= -3
            join bw in dbContext.BaseWeights on adj.Uuid equals bw.Uuid
            join n in dbContext.Names on adj.Uuid equals n.Uuid into nameJoin
            from n in nameJoin.DefaultIfEmpty()
            orderby adj.Uuid
            select new { Adjustment = adj, BaseWeight = bw, Name = n != null ? n.Name : null }
        ).ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return new AdjustmentAutoDailyResponse(0, []);
        }

        var adjusted = new List<AdjustmentAutoDailyAdjustedEntry>(rows.Count);
        var recordedAt = DateTime.UtcNow;

        foreach (var row in rows)
        {
            var adj = row.Adjustment;
            var baseWeight = row.BaseWeight;
            var d = ComputeDelta(adj.Trajectory);

            var previousWeight = baseWeight.Weight;
            baseWeight.Weight += d;
            adj.Trajectory = d > 0 ? 2 : -2;

            dbContext.AdjustmentDailyLogs.Add(new AdjustmentDailyLog
            {
                Id = Guid.NewGuid(),
                Uuid = adj.Uuid,
                PreviousWeight = previousWeight,
                NewWeight = baseWeight.Weight,
                Date = recordedAt
            });

            adjusted.Add(new AdjustmentAutoDailyAdjustedEntry(
                adj.Uuid,
                row.Name ?? string.Empty,
                previousWeight,
                baseWeight.Weight));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AdjustmentAutoDailyResponse(adjusted.Count, adjusted);
    }

    internal static int ComputeDelta(int trajectory) =>
        trajectory >= 3 ? trajectory - 2
        : trajectory <= -3 ? trajectory + 2
        : 0;
}
