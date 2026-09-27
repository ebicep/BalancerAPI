using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Business.Services;

public interface IPlayerWeeklyWeightHistoryService
{
    Task<PlayerWeeklyWeightHistoryResponse?> GetAsync(Guid uuid, CancellationToken cancellationToken);
}

public sealed class PlayerWeeklyWeightHistoryService(BalancerDbContext dbContext) : IPlayerWeeklyWeightHistoryService
{
    public async Task<PlayerWeeklyWeightHistoryResponse?> GetAsync(Guid uuid, CancellationToken cancellationToken)
    {
        var hasBaseWeight = await dbContext.BaseWeights.AsNoTracking()
            .AnyAsync(x => x.Uuid == uuid, cancellationToken);
        if (!hasBaseWeight)
        {
            return null;
        }

        var weeks = await dbContext.TimeWeeks.AsNoTracking()
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
        if (weeks.Count == 0)
        {
            return null;
        }

        var currentWeekId = weeks[^1].Id;

        var baseWeekly = await dbContext.BaseWeightsWeekly.AsNoTracking()
            .Where(x => x.Uuid == uuid)
            .ToDictionaryAsync(x => x.WeekStartDate, cancellationToken);

        var specWeekly = await dbContext.ExperimentalSpecWeightsWeekly.AsNoTracking()
            .Where(x => x.Uuid == uuid)
            .ToDictionaryAsync(x => x.WeekStartDate, cancellationToken);

        var liveBase = await dbContext.BaseWeights.AsNoTracking()
            .Where(x => x.Uuid == uuid)
            .Select(x => (int?)x.Weight)
            .FirstOrDefaultAsync(cancellationToken);

        var liveSpec = await dbContext.ExperimentalSpecWeights.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Uuid == uuid, cancellationToken);

        var firstSnapshotWeek = baseWeekly.Count > 0 ? baseWeekly.Keys.Min() : (int?)null;
        var firstWeekId = firstSnapshotWeek ?? currentWeekId;

        var displayName = await dbContext.Names.AsNoTracking()
            .Where(n => n.Uuid == uuid)
            .Select(n => n.Name)
            .OrderBy(n => n)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var runningBase = 0;
        var hasRunningBase = false;
        var runningOffsets = new ExperimentalSpecWeightWeekly { Uuid = uuid, WeekStartDate = 0 };

        var points = new List<WeeklyWeightPoint>();

        foreach (var week in weeks)
        {
            if (week.Id < firstWeekId)
            {
                continue;
            }

            if (baseWeekly.TryGetValue(week.Id, out var baseSnap))
            {
                runningBase = baseSnap.Weight;
                hasRunningBase = true;
            }

            if (specWeekly.TryGetValue(week.Id, out var specSnap))
            {
                CopyOffsets(specSnap, runningOffsets);
            }

            var isCurrentWeek = week.Id == currentWeekId;
            var baseWeight = runningBase;

            if (isCurrentWeek && liveBase is not null)
            {
                baseWeight = liveBase.Value;
                hasRunningBase = true;
            }

            if (!hasRunningBase)
            {
                continue;
            }

            SpecWeightsResponse specWeights;
            if (isCurrentWeek && liveSpec is not null)
            {
                specWeights = ToEffectiveSpecWeights(baseWeight, liveSpec);
            }
            else
            {
                specWeights = ToEffectiveSpecWeights(baseWeight, runningOffsets);
            }

            points.Add(new WeeklyWeightPoint(
                week.Id,
                week.Timestamp,
                isCurrentWeek,
                baseWeight,
                specWeights));
        }

        if (points.Count == 0)
        {
            return null;
        }

        return new PlayerWeeklyWeightHistoryResponse(displayName, uuid, points);
    }

    private static void CopyOffsets(ExperimentalSpecWeightWeekly from, ExperimentalSpecWeightWeekly to)
    {
        to.PyromancerOffset = from.PyromancerOffset;
        to.CryomancerOffset = from.CryomancerOffset;
        to.AquamancerOffset = from.AquamancerOffset;
        to.BerserkerOffset = from.BerserkerOffset;
        to.DefenderOffset = from.DefenderOffset;
        to.RevenantOffset = from.RevenantOffset;
        to.AvengerOffset = from.AvengerOffset;
        to.CrusaderOffset = from.CrusaderOffset;
        to.ProtectorOffset = from.ProtectorOffset;
        to.ThunderlordOffset = from.ThunderlordOffset;
        to.SpiritguardOffset = from.SpiritguardOffset;
        to.EarthwardenOffset = from.EarthwardenOffset;
        to.AssassinOffset = from.AssassinOffset;
        to.VindicatorOffset = from.VindicatorOffset;
        to.ApothecaryOffset = from.ApothecaryOffset;
        to.ConjurerOffset = from.ConjurerOffset;
        to.SentinelOffset = from.SentinelOffset;
        to.LuminaryOffset = from.LuminaryOffset;
    }

    internal static SpecWeightsResponse ToEffectiveSpecWeights(int baseWeight, ExperimentalSpecWeightWeekly offsets) =>
        new(
            baseWeight - offsets.PyromancerOffset,
            baseWeight - offsets.CryomancerOffset,
            baseWeight - offsets.AquamancerOffset,
            baseWeight - offsets.BerserkerOffset,
            baseWeight - offsets.DefenderOffset,
            baseWeight - offsets.RevenantOffset,
            baseWeight - offsets.AvengerOffset,
            baseWeight - offsets.CrusaderOffset,
            baseWeight - offsets.ProtectorOffset,
            baseWeight - offsets.ThunderlordOffset,
            baseWeight - offsets.SpiritguardOffset,
            baseWeight - offsets.EarthwardenOffset,
            baseWeight - offsets.AssassinOffset,
            baseWeight - offsets.VindicatorOffset,
            baseWeight - offsets.ApothecaryOffset,
            baseWeight - offsets.ConjurerOffset,
            baseWeight - offsets.SentinelOffset,
            baseWeight - offsets.LuminaryOffset);

    internal static SpecWeightsResponse ToEffectiveSpecWeights(int baseWeight, ExperimentalSpecWeight offsets) =>
        new(
            baseWeight - offsets.PyromancerOffset,
            baseWeight - offsets.CryomancerOffset,
            baseWeight - offsets.AquamancerOffset,
            baseWeight - offsets.BerserkerOffset,
            baseWeight - offsets.DefenderOffset,
            baseWeight - offsets.RevenantOffset,
            baseWeight - offsets.AvengerOffset,
            baseWeight - offsets.CrusaderOffset,
            baseWeight - offsets.ProtectorOffset,
            baseWeight - offsets.ThunderlordOffset,
            baseWeight - offsets.SpiritguardOffset,
            baseWeight - offsets.EarthwardenOffset,
            baseWeight - offsets.AssassinOffset,
            baseWeight - offsets.VindicatorOffset,
            baseWeight - offsets.ApothecaryOffset,
            baseWeight - offsets.ConjurerOffset,
            baseWeight - offsets.SentinelOffset,
            baseWeight - offsets.LuminaryOffset);
}
