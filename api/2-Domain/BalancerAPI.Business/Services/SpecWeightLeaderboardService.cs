using System.Collections.Concurrent;
using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Business.Services;

public sealed class SpecWeightLeaderboardService(
    IDbContextFactory<BalancerDbContext> dbContextFactory) : ISpecWeightLeaderboardService
{
    private sealed record PlayerRow(Guid Uuid, string Name, int[] Weights);

    public async Task<Dictionary<string, IReadOnlyList<SpecWeightLeaderboardEntry>>> GetLeaderboardAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var playerDataTask = LoadPlayerDataAsync(cancellationToken);
        var specBansTask = LoadSpecBansAsync(cancellationToken);
        await Task.WhenAll(playerDataTask, specBansTask);

        var players = await playerDataTask;
        var specBansByPlayer = await specBansTask;
        var skip = (page - 1) * pageSize;
        var result = new ConcurrentDictionary<string, IReadOnlyList<SpecWeightLeaderboardEntry>>(
            StringComparer.Ordinal);

        Parallel.For(0, ExperimentalSpecs.AllOrdered.Length, specIndex =>
        {
            var spec = ExperimentalSpecs.AllOrdered[specIndex];
            var ranked = players
                .Where(p => !IsBanned(p.Uuid, specIndex, specBansByPlayer))
                .Select(p => new
                {
                    p.Uuid,
                    p.Name,
                    Weight = p.Weights[specIndex]
                })
                .OrderByDescending(x => x.Weight)
                .ThenBy(x => x.Name, StringComparer.Ordinal)
                .ThenBy(x => x.Uuid)
                .Skip(skip)
                .Take(pageSize)
                .Select(x => new SpecWeightLeaderboardEntry
                {
                    Uuid = x.Uuid.ToString(),
                    Name = x.Name,
                    SpecWeight = x.Weight
                })
                .ToList();

            result[spec.ToLowerInvariant()] = ranked;
        });

        return ExperimentalSpecs.AllOrdered
            .ToDictionary(
                spec => spec.ToLowerInvariant(),
                spec => result[spec.ToLowerInvariant()],
                StringComparer.Ordinal);
    }

    private async Task<IReadOnlyList<PlayerRow>> LoadPlayerDataAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.ExperimentalBalancePlayerData
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return DedupeByUuid(rows)
            .Select(row => new PlayerRow(row.Uuid, row.Name, BuildWeightVector(row)))
            .ToList();
    }

    private async Task<IReadOnlyDictionary<Guid, bool[]>> LoadSpecBansAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.ExperimentalSpecBans
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var dict = new Dictionary<Guid, bool[]>(rows.Count);
        foreach (var row in rows)
        {
            var vec = BanVectorFromRow(row);
            if (!AnyBanFlag(vec))
            {
                continue;
            }

            dict[row.Uuid] = vec;
        }

        return dict;
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

    private static bool[] BanVectorFromRow(ExperimentalSpecBan row) =>
    [
        row.Pyromancer,
        row.Cryomancer,
        row.Aquamancer,
        row.Berserker,
        row.Defender,
        row.Revenant,
        row.Avenger,
        row.Crusader,
        row.Protector,
        row.Thunderlord,
        row.Spiritguard,
        row.Earthwarden,
        row.Assassin,
        row.Vindicator,
        row.Apothecary,
        row.Conjurer,
        row.Sentinel,
        row.Luminary
    ];

    private static bool AnyBanFlag(bool[] vec)
    {
        foreach (var b in vec)
        {
            if (b)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsBanned(
        Guid playerId,
        int allOrderedIndex,
        IReadOnlyDictionary<Guid, bool[]> specBansByPlayer)
    {
        if (!specBansByPlayer.TryGetValue(playerId, out var banVec))
        {
            return false;
        }

        return (uint)allOrderedIndex < (uint)banVec.Length && banVec[allOrderedIndex];
    }
}
