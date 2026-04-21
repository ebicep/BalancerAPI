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
            join specWeight in dbContext.ExperimentalSpecWeights on wl.Uuid equals specWeight.Uuid
            join baseWeight in dbContext.BaseWeights.AsNoTracking() on wl.Uuid equals baseWeight.Uuid
            join n in dbContext.Names.AsNoTracking() on wl.Uuid equals n.Uuid into nameJoin
            from n in nameJoin.DefaultIfEmpty()
            orderby wl.Uuid
            select new { wl, specWeight, baseWeight, Name = n != null ? n.Name : null }
        ).ToListAsync(cancellationToken);

        if (joinedRows.Count == 0)
        {
            return new AdjustmentAutoWeeklyResponse(0, []);
        }

        var adjusted = new Dictionary<Guid, AdjustmentAutoWeeklyPlayerBlock>();
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
            var specWeight = row.specWeight;
            var baseWeight = row.baseWeight;
            var displayName = row.Name ?? string.Empty;

            var specChanges = new List<AdjustmentAutoWeeklySpecChange>();

            foreach (var spec in ExperimentalSpecs.AllOrdered)
            {
                var (wins, losses) = GetWinsLosses(wl, spec);
                var adjustment = ComputeWeeklySpecOffsetAdjustment(wins, losses);
                if (adjustment == 0)
                {
                    continue;
                }

                var previousOffset = GetOffset(specWeight, spec);
                ApplyOffsetAdjustment(specWeight, spec, adjustment);
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

            adjusted[wl.Uuid] = new AdjustmentAutoWeeklyPlayerBlock(
                displayName,
                baseWeight.Weight,
                specChanges);
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

    private static int GetOffset(ExperimentalSpecWeight sw, string spec) =>
        spec switch
        {
            "Pyromancer" => sw.PyromancerOffset,
            "Cryomancer" => sw.CryomancerOffset,
            "Aquamancer" => sw.AquamancerOffset,
            "Berserker" => sw.BerserkerOffset,
            "Defender" => sw.DefenderOffset,
            "Revenant" => sw.RevenantOffset,
            "Avenger" => sw.AvengerOffset,
            "Crusader" => sw.CrusaderOffset,
            "Protector" => sw.ProtectorOffset,
            "Thunderlord" => sw.ThunderlordOffset,
            "Spiritguard" => sw.SpiritguardOffset,
            "Earthwarden" => sw.EarthwardenOffset,
            "Assassin" => sw.AssassinOffset,
            "Vindicator" => sw.VindicatorOffset,
            "Apothecary" => sw.ApothecaryOffset,
            "Conjurer" => sw.ConjurerOffset,
            "Sentinel" => sw.SentinelOffset,
            "Luminary" => sw.LuminaryOffset,
            _ => 0
        };

    /// <summary>Apply <c>offset -= adjustment</c> (winning week lowers offset; losing week raises it).</summary>
    private static void ApplyOffsetAdjustment(ExperimentalSpecWeight sw, string spec, int adjustment)
    {
        switch (spec)
        {
            case "Pyromancer":
                sw.PyromancerOffset -= adjustment;
                break;
            case "Cryomancer":
                sw.CryomancerOffset -= adjustment;
                break;
            case "Aquamancer":
                sw.AquamancerOffset -= adjustment;
                break;
            case "Berserker":
                sw.BerserkerOffset -= adjustment;
                break;
            case "Defender":
                sw.DefenderOffset -= adjustment;
                break;
            case "Revenant":
                sw.RevenantOffset -= adjustment;
                break;
            case "Avenger":
                sw.AvengerOffset -= adjustment;
                break;
            case "Crusader":
                sw.CrusaderOffset -= adjustment;
                break;
            case "Protector":
                sw.ProtectorOffset -= adjustment;
                break;
            case "Thunderlord":
                sw.ThunderlordOffset -= adjustment;
                break;
            case "Spiritguard":
                sw.SpiritguardOffset -= adjustment;
                break;
            case "Earthwarden":
                sw.EarthwardenOffset -= adjustment;
                break;
            case "Assassin":
                sw.AssassinOffset -= adjustment;
                break;
            case "Vindicator":
                sw.VindicatorOffset -= adjustment;
                break;
            case "Apothecary":
                sw.ApothecaryOffset -= adjustment;
                break;
            case "Conjurer":
                sw.ConjurerOffset -= adjustment;
                break;
            case "Sentinel":
                sw.SentinelOffset -= adjustment;
                break;
            case "Luminary":
                sw.LuminaryOffset -= adjustment;
                break;
        }
    }
}
