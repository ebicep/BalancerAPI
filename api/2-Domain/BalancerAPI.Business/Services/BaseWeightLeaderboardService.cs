using BalancerAPI.Data.Data;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Business.Services;

public sealed class BaseWeightLeaderboardService(
    IDbContextFactory<BalancerDbContext> dbContextFactory) : IBaseWeightLeaderboardService
{
    public async Task<IReadOnlyList<BaseWeightLeaderboardEntry>> GetLeaderboardAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var skip = (page - 1) * pageSize;

        return await db.BaseWeights
            .AsNoTracking()
            .Join(
                db.Names.AsNoTracking(),
                bw => bw.Uuid,
                n => n.Uuid,
                (bw, n) => new { bw.Uuid, n.Name, bw.Weight })
            .OrderByDescending(x => x.Weight)
            .ThenBy(x => x.Name)
            .ThenBy(x => x.Uuid)
            .Skip(skip)
            .Take(pageSize)
            .Select(x => new BaseWeightLeaderboardEntry
            {
                Uuid = x.Uuid.ToString(),
                Name = x.Name,
                BaseWeight = x.Weight
            })
            .ToListAsync(cancellationToken);
    }
}
