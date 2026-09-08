using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Business.Services;

public sealed class AverageSpecWeightLeaderboardService(
    IDbContextFactory<BalancerDbContext> dbContextFactory) : IAverageSpecWeightLeaderboardService
{
    public async Task<IReadOnlyList<AverageSpecWeightLeaderboardEntry>> GetLeaderboardAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var cutoff = DateTime.UtcNow.AddMonths(-1);
        var activeUuids = await db.BaseWeights
            .AsNoTracking()
            .Where(bw => bw.LastPlayed != null && bw.LastPlayed >= cutoff)
            .Select(bw => bw.Uuid)
            .ToListAsync(cancellationToken);
        var activeSet = activeUuids.ToHashSet();

        var rows = await db.ExperimentalBalancePlayerData
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var skip = (page - 1) * pageSize;

        return DedupeByUuid(rows)
            .Where(row => activeSet.Contains(row.Uuid))
            .Select(row =>
            {
                var vec = BuildWeightVector(row);
                var average = Math.Round(vec.Average(), 2, MidpointRounding.AwayFromZero);
                return new
                {
                    row.Uuid,
                    row.Name,
                    AverageWeight = average
                };
            })
            .OrderByDescending(x => x.AverageWeight)
            .ThenBy(x => x.Name, StringComparer.Ordinal)
            .ThenBy(x => x.Uuid)
            .Skip(skip)
            .Take(pageSize)
            .Select(x => new AverageSpecWeightLeaderboardEntry
            {
                Uuid = x.Uuid.ToString(),
                Name = x.Name,
                AverageWeight = x.AverageWeight
            })
            .ToList();
    }

    private static List<ExperimentalBalancePlayerData> DedupeByUuid(
        IReadOnlyList<ExperimentalBalancePlayerData> rows) =>
        rows
            .GroupBy(r => r.Uuid)
            .Select(g => g
                .OrderByDescending(r => string.IsNullOrEmpty(r.Name) ? 0 : 1)
                .ThenBy(r => r.Name, StringComparer.Ordinal)
                .First())
            .ToList();

    private static int[] BuildWeightVector(ExperimentalBalancePlayerData row) =>
    [
        row.PyromancerWeight,
        row.CryomancerWeight,
        row.AquamancerWeight,
        row.BerserkerWeight,
        row.DefenderWeight,
        row.RevenantWeight,
        row.AvengerWeight,
        row.CrusaderWeight,
        row.ProtectorWeight,
        row.ThunderlordWeight,
        row.SpiritguardWeight,
        row.EarthwardenWeight,
        row.AssassinWeight,
        row.VindicatorWeight,
        row.ApothecaryWeight,
        row.ConjurerWeight,
        row.SentinelWeight,
        row.LuminaryWeight
    ];
}
