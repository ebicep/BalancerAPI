using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Business.Services;

public sealed class WeeklyAdjustPatchService(BalancerDbContext dbContext) : IWeeklyAdjustPatchService
{
    public async Task<WeeklyAdjustPatchResponse> ApplyPatchAsync(CancellationToken cancellationToken)
    {
        var cutoff = WeeklyAdjustPatchCutoff.Utc;
        var recordedAt = DateTime.UtcNow;

        var autoRows = await dbContext.AdjustmentWeeklyLogs
            .AsNoTracking()
            .Where(x => x.Date < cutoff && x.WeekKey != -1)
            .GroupBy(x => new { x.Uuid, x.Spec })
            .Select(g => new
            {
                g.Key.Uuid,
                g.Key.Spec,
                AutoSpecDelta = g.Sum(x => x.Adjusted)
            })
            .ToListAsync(cancellationToken);

        var manualGrouped = await dbContext.AdjustmentManualWeeklyLogs
            .AsNoTracking()
            .Where(x => x.Date < cutoff)
            .GroupBy(x => new { x.Uuid, x.Spec })
            .Select(g => new
            {
                g.Key.Uuid,
                g.Key.Spec,
                ManualSpecDelta = g.Sum(x => x.NewSpecWeight - x.PreviousSpecWeight),
                HasManual = true
            })
            .ToListAsync(cancellationToken);

        var manualByKey = manualGrouped.ToDictionary(x => (x.Uuid, x.Spec));
        var autoByKey = autoRows.ToDictionary(x => (x.Uuid, x.Spec));

        var candidateKeys = autoByKey.Keys
            .Union(manualByKey.Keys)
            .Where(k =>
            {
                var auto = autoByKey.GetValueOrDefault(k)?.AutoSpecDelta ?? 0;
                return auto != 0 || manualByKey.ContainsKey(k);
            })
            .OrderBy(k => k.Uuid)
            .ThenBy(k => k.Spec, StringComparer.Ordinal)
            .ToList();

        var entries = new List<WeeklyAdjustPatchEntry>();
        var weeklyLogs = new List<AdjustmentWeeklyLog>();
        var touchedSpecWeights = new Dictionary<Guid, ExperimentalSpecWeight>();

        foreach (var (uuid, spec) in candidateKeys)
        {
            var autoSpecDelta = autoByKey.GetValueOrDefault((uuid, spec))?.AutoSpecDelta ?? 0;
            var hasManual = manualByKey.TryGetValue((uuid, spec), out var manualAgg);
            var manualSpecDelta = hasManual ? manualAgg!.ManualSpecDelta : 0;

            var (patchAdjusted, executed, netSpecDelta) = ComputePatch(autoSpecDelta, manualSpecDelta, hasManual);

            var row = await (
                from sw in dbContext.ExperimentalSpecWeights
                where sw.Uuid == uuid
                join bw in dbContext.BaseWeights.AsNoTracking() on sw.Uuid equals bw.Uuid
                join n in dbContext.Names.AsNoTracking() on sw.Uuid equals n.Uuid into nameJoin
                from n in nameJoin.DefaultIfEmpty()
                select new
                {
                    SpecWeight = sw,
                    bw.Weight,
                    Name = n != null ? n.Name : null
                }
            ).AsTracking().FirstOrDefaultAsync(cancellationToken);

            if (row is null)
            {
                continue;
            }

            var specWeight = row.SpecWeight;
            var baseWeight = row.Weight;
            var displayName = row.Name ?? string.Empty;
            var previousOffset = ExperimentalSpecOffsetHelpers.GetOffset(specWeight, spec);
            var previousWeight = baseWeight - previousOffset;
            var currentOffset = executed ? previousOffset - patchAdjusted : previousOffset;
            var currentWeight = baseWeight - currentOffset;
            var weightChange = currentWeight - previousWeight;

            entries.Add(new WeeklyAdjustPatchEntry(
                uuid,
                displayName,
                spec,
                autoSpecDelta,
                manualSpecDelta,
                netSpecDelta,
                patchAdjusted,
                executed,
                previousOffset,
                currentOffset,
                previousWeight,
                currentWeight,
                weightChange,
                baseWeight));

            if (!executed)
            {
                continue;
            }

            weeklyLogs.Add(new AdjustmentWeeklyLog
            {
                Id = Guid.NewGuid(),
                WeekKey = -1,
                Uuid = uuid,
                Spec = spec,
                Wins = 0,
                Losses = 0,
                Adjusted = patchAdjusted,
                PreviousWeight = previousWeight,
                PreviousOffset = previousOffset,
                Date = recordedAt
            });

            ExperimentalSpecOffsetHelpers.ApplyOffsetAdjustment(specWeight, spec, patchAdjusted);
            touchedSpecWeights[uuid] = specWeight;
        }

        foreach (var specWeight in touchedSpecWeights.Values)
        {
            specWeight.LastUpdated = recordedAt;
        }

        dbContext.AdjustmentWeeklyLogs.AddRange(weeklyLogs);
        await dbContext.SaveChangesAsync(cancellationToken);

        var executedCount = entries.Count(x => x.Executed);
        return new WeeklyAdjustPatchResponse(executedCount, recordedAt, cutoff, entries);
    }

    internal static (int PatchAdjusted, bool Executed, int NetSpecDelta) ComputePatch(
        int autoSpecDelta,
        int manualSpecDelta,
        bool hasManual)
    {
        if (!hasManual)
        {
            return (autoSpecDelta, autoSpecDelta != 0, autoSpecDelta);
        }

        var netSpecDelta = autoSpecDelta - manualSpecDelta;
        return (netSpecDelta, netSpecDelta < 0, netSpecDelta);
    }
}
