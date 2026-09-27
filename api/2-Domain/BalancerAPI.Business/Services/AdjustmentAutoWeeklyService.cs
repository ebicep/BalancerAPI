using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Business.Services;

public sealed class AdjustmentAutoWeeklyService(BalancerDbContext dbContext) : IAdjustmentAutoWeeklyService
{
    public async Task<AdjustmentAutoWeeklyResponse> ApplyAutoWeeklyAsync(CancellationToken cancellationToken)
    {
        var joinedRows = await (
            from wl in dbContext.ExperimentalSpecsWlCurrentWeek.AsNoTracking()
            join specWeight in dbContext.ExperimentalSpecWeights.AsNoTracking() on wl.Uuid equals specWeight.Uuid
            join baseWeight in dbContext.BaseWeights.AsNoTracking() on wl.Uuid equals baseWeight.Uuid
            join n in dbContext.Names.AsNoTracking() on wl.Uuid equals n.Uuid into nameJoin
            from n in nameJoin.DefaultIfEmpty()
            orderby wl.Uuid
            select new { wl, baseWeight, Name = n != null ? n.Name : null }
        ).ToListAsync(cancellationToken);

        if (joinedRows.Count == 0)
        {
            return new AdjustmentAutoWeeklyResponse(0, []);
        }

        var adjusted = new List<AdjustmentAutoWeeklyPlayerBlock>();
        var weeklyLogs = new List<AdjustmentWeeklyLog>();
        var recordedAt = DateTime.UtcNow;
        var weekKey = await dbContext.TimeWeeks
            .AsNoTracking()
            .OrderByDescending(x => x.Id)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken) ?? 0;

        foreach (var row in joinedRows.DistinctBy(x => x.wl.Uuid))
        {
            var wl = row.wl;
            var baseWeight = row.baseWeight;
            var displayName = row.Name ?? string.Empty;
            var specWeight = await dbContext.ExperimentalSpecWeights
                .SingleAsync(x => x.Uuid == wl.Uuid, cancellationToken);

            var specChanges = new List<AdjustmentAutoWeeklySpecChange>();

            foreach (var spec in ExperimentalSpecs.AllOrdered)
            {
                var (wins, losses) = GetWinsLosses(wl, spec);
                var adjustment = ComputeWeeklySpecOffsetAdjustment(wins, losses);
                if (adjustment == 0)
                {
                    continue;
                }

                var previousOffset = ExperimentalSpecOffsetHelpers.GetOffset(specWeight, spec);
                ExperimentalSpecOffsetHelpers.ApplyOffsetAdjustment(specWeight, spec, adjustment);
                var currentOffset = previousOffset - adjustment;

                specChanges.Add(new AdjustmentAutoWeeklySpecChange(
                    spec,
                    baseWeight.Weight - previousOffset,
                    baseWeight.Weight - currentOffset,
                    previousOffset,
                    currentOffset));

                weeklyLogs.Add(new AdjustmentWeeklyLog
                {
                    Id = Guid.NewGuid(),
                    WeekKey = weekKey,
                    Uuid = wl.Uuid,
                    Spec = spec,
                    Wins = wins,
                    Losses = losses,
                    Adjusted = adjustment,
                    PreviousWeight = baseWeight.Weight - previousOffset,
                    PreviousOffset = previousOffset,
                    Date = recordedAt
                });
            }

            if (specChanges.Count == 0)
            {
                continue;
            }

            specWeight.LastUpdated = recordedAt;

            adjusted.Add(new AdjustmentAutoWeeklyPlayerBlock(
                wl.Uuid,
                displayName,
                baseWeight.Weight,
                specChanges));
        }

        if (adjusted.Count == 0)
        {
            return new AdjustmentAutoWeeklyResponse(0, []);
        }

        dbContext.AdjustmentWeeklyLogs.AddRange(weeklyLogs);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AdjustmentAutoWeeklyResponse(adjusted.Count, adjusted);
    }

    /// <summary>
    /// Signed adjustment applied as <c>offset -= adjustment</c>: strong winning week (net &gt; 2) returns positive;
    /// strong losing week (net &lt; -2) returns negative.
    /// </summary>
    internal static int ComputeWeeklySpecOffsetAdjustment(int wins, int losses)
    {
        var net = wins - losses;
        return net switch
        {
            > 2 => net - 2,
            < -2 => net + 2,
            _ => 0
        };
    }

    private static (int Wins, int Losses) GetWinsLosses(ExperimentalSpecsWlCurrentWeek wl, string spec) =>
        spec switch
        {
            "Pyromancer" => (wl.PyromancerWins, wl.PyromancerLosses),
            "Cryomancer" => (wl.CryomancerWins, wl.CryomancerLosses),
            "Aquamancer" => (wl.AquamancerWins, wl.AquamancerLosses),
            "Berserker" => (wl.BerserkerWins, wl.BerserkerLosses),
            "Defender" => (wl.DefenderWins, wl.DefenderLosses),
            "Revenant" => (wl.RevenantWins, wl.RevenantLosses),
            "Avenger" => (wl.AvengerWins, wl.AvengerLosses),
            "Crusader" => (wl.CrusaderWins, wl.CrusaderLosses),
            "Protector" => (wl.ProtectorWins, wl.ProtectorLosses),
            "Thunderlord" => (wl.ThunderlordWins, wl.ThunderlordLosses),
            "Spiritguard" => (wl.SpiritguardWins, wl.SpiritguardLosses),
            "Earthwarden" => (wl.EarthwardenWins, wl.EarthwardenLosses),
            "Assassin" => (wl.AssassinWins, wl.AssassinLosses),
            "Vindicator" => (wl.VindicatorWins, wl.VindicatorLosses),
            "Apothecary" => (wl.ApothecaryWins, wl.ApothecaryLosses),
            "Conjurer" => (wl.ConjurerWins, wl.ConjurerLosses),
            "Sentinel" => (wl.SentinelWins, wl.SentinelLosses),
            "Luminary" => (wl.LuminaryWins, wl.LuminaryLosses),
            _ => (0, 0)
        };

}
