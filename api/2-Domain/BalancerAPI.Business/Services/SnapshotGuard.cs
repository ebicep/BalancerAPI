using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Business.Services;

internal static class SnapshotGuard
{
    public static async Task EnsureBaseWeightDailyAsync(
        BalancerDbContext db,
        IReadOnlyCollection<Guid> uuids,
        CancellationToken cancellationToken)
    {
        var currentDayId = await db.TimeDays
            .OrderByDescending(x => x.Id)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (currentDayId is null) return;

        var alreadySnapshotted = await db.BaseWeightsDaily
            .Where(x => x.DayStartDate == currentDayId && uuids.Contains(x.Uuid))
            .Select(x => x.Uuid)
            .ToHashSetAsync(cancellationToken);

        var toSnapshot = uuids.Where(u => !alreadySnapshotted.Contains(u)).ToList();
        if (toSnapshot.Count == 0) return;

        var rows = await db.BaseWeights
            .Where(x => toSnapshot.Contains(x.Uuid))
            .ToListAsync(cancellationToken);

        db.BaseWeightsDaily.AddRange(rows.Select(r => new BaseWeightDaily
        {
            Uuid = r.Uuid,
            DayStartDate = currentDayId.Value,
            Weight = r.Weight
        }));
    }

    public static async Task EnsureBaseWeightWeeklyAsync(
        BalancerDbContext db,
        IReadOnlyCollection<Guid> uuids,
        CancellationToken cancellationToken)
    {
        var currentWeekId = await db.TimeWeeks
            .OrderByDescending(x => x.Id)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (currentWeekId is null) return;

        var alreadySnapshotted = await db.BaseWeightsWeekly
            .Where(x => x.WeekStartDate == currentWeekId && uuids.Contains(x.Uuid))
            .Select(x => x.Uuid)
            .ToHashSetAsync(cancellationToken);

        var toSnapshot = uuids.Where(u => !alreadySnapshotted.Contains(u)).ToList();
        if (toSnapshot.Count == 0) return;

        var rows = await db.BaseWeights
            .Where(x => toSnapshot.Contains(x.Uuid))
            .ToListAsync(cancellationToken);

        db.BaseWeightsWeekly.AddRange(rows.Select(r => new BaseWeightWeekly
        {
            Uuid = r.Uuid,
            WeekStartDate = currentWeekId.Value,
            Weight = r.Weight
        }));
    }

    public static async Task EnsureSpecsWlDailyAsync(
        BalancerDbContext db,
        IReadOnlyCollection<Guid> uuids,
        CancellationToken cancellationToken)
    {
        var currentDayId = await db.TimeDays
            .OrderByDescending(x => x.Id)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (currentDayId is null) return;

        var alreadySnapshotted = await db.ExperimentalSpecsWlDaily
            .Where(x => x.DayStartDate == currentDayId && uuids.Contains(x.Uuid))
            .Select(x => x.Uuid)
            .ToHashSetAsync(cancellationToken);

        var toSnapshot = uuids.Where(u => !alreadySnapshotted.Contains(u)).ToList();
        if (toSnapshot.Count == 0) return;

        var rows = await db.ExperimentalSpecsWl
            .Where(x => toSnapshot.Contains(x.Uuid))
            .ToListAsync(cancellationToken);
        var uncountRows = await db.ExperimentalSpecsWlUncount
            .Where(x => toSnapshot.Contains(x.Uuid))
            .ToDictionaryAsync(x => x.Uuid, cancellationToken);

        db.ExperimentalSpecsWlDaily.AddRange(
            rows.Select(r => ToDailySnapshot(r, uncountRows.GetValueOrDefault(r.Uuid), currentDayId.Value)));
    }

    public static async Task EnsureSpecsWlWeeklyAsync(
        BalancerDbContext db,
        IReadOnlyCollection<Guid> uuids,
        CancellationToken cancellationToken)
    {
        var currentWeekId = await db.TimeWeeks
            .OrderByDescending(x => x.Id)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (currentWeekId is null) return;

        var alreadySnapshotted = await db.ExperimentalSpecsWlWeekly
            .Where(x => x.WeekStartDate == currentWeekId && uuids.Contains(x.Uuid))
            .Select(x => x.Uuid)
            .ToHashSetAsync(cancellationToken);

        var toSnapshot = uuids.Where(u => !alreadySnapshotted.Contains(u)).ToList();
        if (toSnapshot.Count == 0) return;

        var rows = await db.ExperimentalSpecsWl
            .Where(x => toSnapshot.Contains(x.Uuid))
            .ToListAsync(cancellationToken);
        var uncountRows = await db.ExperimentalSpecsWlUncount
            .Where(x => toSnapshot.Contains(x.Uuid))
            .ToDictionaryAsync(x => x.Uuid, cancellationToken);

        db.ExperimentalSpecsWlWeekly.AddRange(
            rows.Select(r => ToWeeklySnapshot(r, uncountRows.GetValueOrDefault(r.Uuid), currentWeekId.Value)));
    }

    public static async Task EnsureSpecWeightsWeeklyAsync(
        BalancerDbContext db,
        IReadOnlyCollection<Guid> uuids,
        CancellationToken cancellationToken)
    {
        var currentWeekId = await db.TimeWeeks
            .OrderByDescending(x => x.Id)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (currentWeekId is null) return;

        var alreadySnapshotted = await db.ExperimentalSpecWeightsWeekly
            .Where(x => x.WeekStartDate == currentWeekId && uuids.Contains(x.Uuid))
            .Select(x => x.Uuid)
            .ToHashSetAsync(cancellationToken);

        var toSnapshot = uuids.Where(u => !alreadySnapshotted.Contains(u)).ToList();
        if (toSnapshot.Count == 0) return;

        var rows = await db.ExperimentalSpecWeights
            .Where(x => toSnapshot.Contains(x.Uuid))
            .ToListAsync(cancellationToken);

        db.ExperimentalSpecWeightsWeekly.AddRange(rows.Select(r => new ExperimentalSpecWeightWeekly
        {
            Uuid = r.Uuid,
            WeekStartDate = currentWeekId.Value,
            PyromancerOffset = r.PyromancerOffset,
            CryomancerOffset = r.CryomancerOffset,
            AquamancerOffset = r.AquamancerOffset,
            BerserkerOffset = r.BerserkerOffset,
            DefenderOffset = r.DefenderOffset,
            RevenantOffset = r.RevenantOffset,
            AvengerOffset = r.AvengerOffset,
            CrusaderOffset = r.CrusaderOffset,
            ProtectorOffset = r.ProtectorOffset,
            ThunderlordOffset = r.ThunderlordOffset,
            SpiritguardOffset = r.SpiritguardOffset,
            EarthwardenOffset = r.EarthwardenOffset,
            AssassinOffset = r.AssassinOffset,
            VindicatorOffset = r.VindicatorOffset,
            ApothecaryOffset = r.ApothecaryOffset,
            ConjurerOffset = r.ConjurerOffset,
            SentinelOffset = r.SentinelOffset,
            LuminaryOffset = r.LuminaryOffset
        }));
    }

    /// <summary>
    /// Snapshots the player's cumulative counted plus uncounted stats, so period deltas taken against this
    /// baseline include games recorded with <c>uncount</c>.
    /// </summary>
    public static ExperimentalSpecsWlDaily ToDailySnapshot(
        ExperimentalSpecsWlColumns c,
        ExperimentalSpecsWlColumns? u,
        int dayStartDate) =>
        new()
        {
            Uuid = c.Uuid,
            DayStartDate = dayStartDate,
            PyromancerWins = c.PyromancerWins + (u?.PyromancerWins ?? 0),
            PyromancerLosses = c.PyromancerLosses + (u?.PyromancerLosses ?? 0),
            PyromancerKills = c.PyromancerKills + (u?.PyromancerKills ?? 0),
            PyromancerDeaths = c.PyromancerDeaths + (u?.PyromancerDeaths ?? 0),
            CryomancerWins = c.CryomancerWins + (u?.CryomancerWins ?? 0),
            CryomancerLosses = c.CryomancerLosses + (u?.CryomancerLosses ?? 0),
            CryomancerKills = c.CryomancerKills + (u?.CryomancerKills ?? 0),
            CryomancerDeaths = c.CryomancerDeaths + (u?.CryomancerDeaths ?? 0),
            AquamancerWins = c.AquamancerWins + (u?.AquamancerWins ?? 0),
            AquamancerLosses = c.AquamancerLosses + (u?.AquamancerLosses ?? 0),
            AquamancerKills = c.AquamancerKills + (u?.AquamancerKills ?? 0),
            AquamancerDeaths = c.AquamancerDeaths + (u?.AquamancerDeaths ?? 0),
            BerserkerWins = c.BerserkerWins + (u?.BerserkerWins ?? 0),
            BerserkerLosses = c.BerserkerLosses + (u?.BerserkerLosses ?? 0),
            BerserkerKills = c.BerserkerKills + (u?.BerserkerKills ?? 0),
            BerserkerDeaths = c.BerserkerDeaths + (u?.BerserkerDeaths ?? 0),
            DefenderWins = c.DefenderWins + (u?.DefenderWins ?? 0),
            DefenderLosses = c.DefenderLosses + (u?.DefenderLosses ?? 0),
            DefenderKills = c.DefenderKills + (u?.DefenderKills ?? 0),
            DefenderDeaths = c.DefenderDeaths + (u?.DefenderDeaths ?? 0),
            RevenantWins = c.RevenantWins + (u?.RevenantWins ?? 0),
            RevenantLosses = c.RevenantLosses + (u?.RevenantLosses ?? 0),
            RevenantKills = c.RevenantKills + (u?.RevenantKills ?? 0),
            RevenantDeaths = c.RevenantDeaths + (u?.RevenantDeaths ?? 0),
            AvengerWins = c.AvengerWins + (u?.AvengerWins ?? 0),
            AvengerLosses = c.AvengerLosses + (u?.AvengerLosses ?? 0),
            AvengerKills = c.AvengerKills + (u?.AvengerKills ?? 0),
            AvengerDeaths = c.AvengerDeaths + (u?.AvengerDeaths ?? 0),
            CrusaderWins = c.CrusaderWins + (u?.CrusaderWins ?? 0),
            CrusaderLosses = c.CrusaderLosses + (u?.CrusaderLosses ?? 0),
            CrusaderKills = c.CrusaderKills + (u?.CrusaderKills ?? 0),
            CrusaderDeaths = c.CrusaderDeaths + (u?.CrusaderDeaths ?? 0),
            ProtectorWins = c.ProtectorWins + (u?.ProtectorWins ?? 0),
            ProtectorLosses = c.ProtectorLosses + (u?.ProtectorLosses ?? 0),
            ProtectorKills = c.ProtectorKills + (u?.ProtectorKills ?? 0),
            ProtectorDeaths = c.ProtectorDeaths + (u?.ProtectorDeaths ?? 0),
            ThunderlordWins = c.ThunderlordWins + (u?.ThunderlordWins ?? 0),
            ThunderlordLosses = c.ThunderlordLosses + (u?.ThunderlordLosses ?? 0),
            ThunderlordKills = c.ThunderlordKills + (u?.ThunderlordKills ?? 0),
            ThunderlordDeaths = c.ThunderlordDeaths + (u?.ThunderlordDeaths ?? 0),
            SpiritguardWins = c.SpiritguardWins + (u?.SpiritguardWins ?? 0),
            SpiritguardLosses = c.SpiritguardLosses + (u?.SpiritguardLosses ?? 0),
            SpiritguardKills = c.SpiritguardKills + (u?.SpiritguardKills ?? 0),
            SpiritguardDeaths = c.SpiritguardDeaths + (u?.SpiritguardDeaths ?? 0),
            EarthwardenWins = c.EarthwardenWins + (u?.EarthwardenWins ?? 0),
            EarthwardenLosses = c.EarthwardenLosses + (u?.EarthwardenLosses ?? 0),
            EarthwardenKills = c.EarthwardenKills + (u?.EarthwardenKills ?? 0),
            EarthwardenDeaths = c.EarthwardenDeaths + (u?.EarthwardenDeaths ?? 0),
            AssassinWins = c.AssassinWins + (u?.AssassinWins ?? 0),
            AssassinLosses = c.AssassinLosses + (u?.AssassinLosses ?? 0),
            AssassinKills = c.AssassinKills + (u?.AssassinKills ?? 0),
            AssassinDeaths = c.AssassinDeaths + (u?.AssassinDeaths ?? 0),
            VindicatorWins = c.VindicatorWins + (u?.VindicatorWins ?? 0),
            VindicatorLosses = c.VindicatorLosses + (u?.VindicatorLosses ?? 0),
            VindicatorKills = c.VindicatorKills + (u?.VindicatorKills ?? 0),
            VindicatorDeaths = c.VindicatorDeaths + (u?.VindicatorDeaths ?? 0),
            ApothecaryWins = c.ApothecaryWins + (u?.ApothecaryWins ?? 0),
            ApothecaryLosses = c.ApothecaryLosses + (u?.ApothecaryLosses ?? 0),
            ApothecaryKills = c.ApothecaryKills + (u?.ApothecaryKills ?? 0),
            ApothecaryDeaths = c.ApothecaryDeaths + (u?.ApothecaryDeaths ?? 0),
            ConjurerWins = c.ConjurerWins + (u?.ConjurerWins ?? 0),
            ConjurerLosses = c.ConjurerLosses + (u?.ConjurerLosses ?? 0),
            ConjurerKills = c.ConjurerKills + (u?.ConjurerKills ?? 0),
            ConjurerDeaths = c.ConjurerDeaths + (u?.ConjurerDeaths ?? 0),
            SentinelWins = c.SentinelWins + (u?.SentinelWins ?? 0),
            SentinelLosses = c.SentinelLosses + (u?.SentinelLosses ?? 0),
            SentinelKills = c.SentinelKills + (u?.SentinelKills ?? 0),
            SentinelDeaths = c.SentinelDeaths + (u?.SentinelDeaths ?? 0),
            LuminaryWins = c.LuminaryWins + (u?.LuminaryWins ?? 0),
            LuminaryLosses = c.LuminaryLosses + (u?.LuminaryLosses ?? 0),
            LuminaryKills = c.LuminaryKills + (u?.LuminaryKills ?? 0),
            LuminaryDeaths = c.LuminaryDeaths + (u?.LuminaryDeaths ?? 0)
        };

    /// <inheritdoc cref="ToDailySnapshot"/>
    public static ExperimentalSpecsWlWeekly ToWeeklySnapshot(
        ExperimentalSpecsWlColumns c,
        ExperimentalSpecsWlColumns? u,
        int weekStartDate) =>
        new()
        {
            Uuid = c.Uuid,
            WeekStartDate = weekStartDate,
            PyromancerWins = c.PyromancerWins + (u?.PyromancerWins ?? 0),
            PyromancerLosses = c.PyromancerLosses + (u?.PyromancerLosses ?? 0),
            PyromancerKills = c.PyromancerKills + (u?.PyromancerKills ?? 0),
            PyromancerDeaths = c.PyromancerDeaths + (u?.PyromancerDeaths ?? 0),
            CryomancerWins = c.CryomancerWins + (u?.CryomancerWins ?? 0),
            CryomancerLosses = c.CryomancerLosses + (u?.CryomancerLosses ?? 0),
            CryomancerKills = c.CryomancerKills + (u?.CryomancerKills ?? 0),
            CryomancerDeaths = c.CryomancerDeaths + (u?.CryomancerDeaths ?? 0),
            AquamancerWins = c.AquamancerWins + (u?.AquamancerWins ?? 0),
            AquamancerLosses = c.AquamancerLosses + (u?.AquamancerLosses ?? 0),
            AquamancerKills = c.AquamancerKills + (u?.AquamancerKills ?? 0),
            AquamancerDeaths = c.AquamancerDeaths + (u?.AquamancerDeaths ?? 0),
            BerserkerWins = c.BerserkerWins + (u?.BerserkerWins ?? 0),
            BerserkerLosses = c.BerserkerLosses + (u?.BerserkerLosses ?? 0),
            BerserkerKills = c.BerserkerKills + (u?.BerserkerKills ?? 0),
            BerserkerDeaths = c.BerserkerDeaths + (u?.BerserkerDeaths ?? 0),
            DefenderWins = c.DefenderWins + (u?.DefenderWins ?? 0),
            DefenderLosses = c.DefenderLosses + (u?.DefenderLosses ?? 0),
            DefenderKills = c.DefenderKills + (u?.DefenderKills ?? 0),
            DefenderDeaths = c.DefenderDeaths + (u?.DefenderDeaths ?? 0),
            RevenantWins = c.RevenantWins + (u?.RevenantWins ?? 0),
            RevenantLosses = c.RevenantLosses + (u?.RevenantLosses ?? 0),
            RevenantKills = c.RevenantKills + (u?.RevenantKills ?? 0),
            RevenantDeaths = c.RevenantDeaths + (u?.RevenantDeaths ?? 0),
            AvengerWins = c.AvengerWins + (u?.AvengerWins ?? 0),
            AvengerLosses = c.AvengerLosses + (u?.AvengerLosses ?? 0),
            AvengerKills = c.AvengerKills + (u?.AvengerKills ?? 0),
            AvengerDeaths = c.AvengerDeaths + (u?.AvengerDeaths ?? 0),
            CrusaderWins = c.CrusaderWins + (u?.CrusaderWins ?? 0),
            CrusaderLosses = c.CrusaderLosses + (u?.CrusaderLosses ?? 0),
            CrusaderKills = c.CrusaderKills + (u?.CrusaderKills ?? 0),
            CrusaderDeaths = c.CrusaderDeaths + (u?.CrusaderDeaths ?? 0),
            ProtectorWins = c.ProtectorWins + (u?.ProtectorWins ?? 0),
            ProtectorLosses = c.ProtectorLosses + (u?.ProtectorLosses ?? 0),
            ProtectorKills = c.ProtectorKills + (u?.ProtectorKills ?? 0),
            ProtectorDeaths = c.ProtectorDeaths + (u?.ProtectorDeaths ?? 0),
            ThunderlordWins = c.ThunderlordWins + (u?.ThunderlordWins ?? 0),
            ThunderlordLosses = c.ThunderlordLosses + (u?.ThunderlordLosses ?? 0),
            ThunderlordKills = c.ThunderlordKills + (u?.ThunderlordKills ?? 0),
            ThunderlordDeaths = c.ThunderlordDeaths + (u?.ThunderlordDeaths ?? 0),
            SpiritguardWins = c.SpiritguardWins + (u?.SpiritguardWins ?? 0),
            SpiritguardLosses = c.SpiritguardLosses + (u?.SpiritguardLosses ?? 0),
            SpiritguardKills = c.SpiritguardKills + (u?.SpiritguardKills ?? 0),
            SpiritguardDeaths = c.SpiritguardDeaths + (u?.SpiritguardDeaths ?? 0),
            EarthwardenWins = c.EarthwardenWins + (u?.EarthwardenWins ?? 0),
            EarthwardenLosses = c.EarthwardenLosses + (u?.EarthwardenLosses ?? 0),
            EarthwardenKills = c.EarthwardenKills + (u?.EarthwardenKills ?? 0),
            EarthwardenDeaths = c.EarthwardenDeaths + (u?.EarthwardenDeaths ?? 0),
            AssassinWins = c.AssassinWins + (u?.AssassinWins ?? 0),
            AssassinLosses = c.AssassinLosses + (u?.AssassinLosses ?? 0),
            AssassinKills = c.AssassinKills + (u?.AssassinKills ?? 0),
            AssassinDeaths = c.AssassinDeaths + (u?.AssassinDeaths ?? 0),
            VindicatorWins = c.VindicatorWins + (u?.VindicatorWins ?? 0),
            VindicatorLosses = c.VindicatorLosses + (u?.VindicatorLosses ?? 0),
            VindicatorKills = c.VindicatorKills + (u?.VindicatorKills ?? 0),
            VindicatorDeaths = c.VindicatorDeaths + (u?.VindicatorDeaths ?? 0),
            ApothecaryWins = c.ApothecaryWins + (u?.ApothecaryWins ?? 0),
            ApothecaryLosses = c.ApothecaryLosses + (u?.ApothecaryLosses ?? 0),
            ApothecaryKills = c.ApothecaryKills + (u?.ApothecaryKills ?? 0),
            ApothecaryDeaths = c.ApothecaryDeaths + (u?.ApothecaryDeaths ?? 0),
            ConjurerWins = c.ConjurerWins + (u?.ConjurerWins ?? 0),
            ConjurerLosses = c.ConjurerLosses + (u?.ConjurerLosses ?? 0),
            ConjurerKills = c.ConjurerKills + (u?.ConjurerKills ?? 0),
            ConjurerDeaths = c.ConjurerDeaths + (u?.ConjurerDeaths ?? 0),
            SentinelWins = c.SentinelWins + (u?.SentinelWins ?? 0),
            SentinelLosses = c.SentinelLosses + (u?.SentinelLosses ?? 0),
            SentinelKills = c.SentinelKills + (u?.SentinelKills ?? 0),
            SentinelDeaths = c.SentinelDeaths + (u?.SentinelDeaths ?? 0),
            LuminaryWins = c.LuminaryWins + (u?.LuminaryWins ?? 0),
            LuminaryLosses = c.LuminaryLosses + (u?.LuminaryLosses ?? 0),
            LuminaryKills = c.LuminaryKills + (u?.LuminaryKills ?? 0),
            LuminaryDeaths = c.LuminaryDeaths + (u?.LuminaryDeaths ?? 0)
        };
}
