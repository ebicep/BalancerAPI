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

        db.ExperimentalSpecsWlDaily.AddRange(rows.Select(r => ToDailySnapshot(r, currentDayId.Value)));
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

        db.ExperimentalSpecsWlWeekly.AddRange(rows.Select(r => ToWeeklySnapshot(r, currentWeekId.Value)));
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

    private static ExperimentalSpecsWlDaily ToDailySnapshot(ExperimentalSpecsWl x, int dayStartDate) =>
        new()
        {
            Uuid = x.Uuid,
            DayStartDate = dayStartDate,
            PyromancerWins = x.PyromancerWins,
            PyromancerLosses = x.PyromancerLosses,
            PyromancerKills = x.PyromancerKills,
            PyromancerDeaths = x.PyromancerDeaths,
            CryomancerWins = x.CryomancerWins,
            CryomancerLosses = x.CryomancerLosses,
            CryomancerKills = x.CryomancerKills,
            CryomancerDeaths = x.CryomancerDeaths,
            AquamancerWins = x.AquamancerWins,
            AquamancerLosses = x.AquamancerLosses,
            AquamancerKills = x.AquamancerKills,
            AquamancerDeaths = x.AquamancerDeaths,
            BerserkerWins = x.BerserkerWins,
            BerserkerLosses = x.BerserkerLosses,
            BerserkerKills = x.BerserkerKills,
            BerserkerDeaths = x.BerserkerDeaths,
            DefenderWins = x.DefenderWins,
            DefenderLosses = x.DefenderLosses,
            DefenderKills = x.DefenderKills,
            DefenderDeaths = x.DefenderDeaths,
            RevenantWins = x.RevenantWins,
            RevenantLosses = x.RevenantLosses,
            RevenantKills = x.RevenantKills,
            RevenantDeaths = x.RevenantDeaths,
            AvengerWins = x.AvengerWins,
            AvengerLosses = x.AvengerLosses,
            AvengerKills = x.AvengerKills,
            AvengerDeaths = x.AvengerDeaths,
            CrusaderWins = x.CrusaderWins,
            CrusaderLosses = x.CrusaderLosses,
            CrusaderKills = x.CrusaderKills,
            CrusaderDeaths = x.CrusaderDeaths,
            ProtectorWins = x.ProtectorWins,
            ProtectorLosses = x.ProtectorLosses,
            ProtectorKills = x.ProtectorKills,
            ProtectorDeaths = x.ProtectorDeaths,
            ThunderlordWins = x.ThunderlordWins,
            ThunderlordLosses = x.ThunderlordLosses,
            ThunderlordKills = x.ThunderlordKills,
            ThunderlordDeaths = x.ThunderlordDeaths,
            SpiritguardWins = x.SpiritguardWins,
            SpiritguardLosses = x.SpiritguardLosses,
            SpiritguardKills = x.SpiritguardKills,
            SpiritguardDeaths = x.SpiritguardDeaths,
            EarthwardenWins = x.EarthwardenWins,
            EarthwardenLosses = x.EarthwardenLosses,
            EarthwardenKills = x.EarthwardenKills,
            EarthwardenDeaths = x.EarthwardenDeaths,
            AssassinWins = x.AssassinWins,
            AssassinLosses = x.AssassinLosses,
            AssassinKills = x.AssassinKills,
            AssassinDeaths = x.AssassinDeaths,
            VindicatorWins = x.VindicatorWins,
            VindicatorLosses = x.VindicatorLosses,
            VindicatorKills = x.VindicatorKills,
            VindicatorDeaths = x.VindicatorDeaths,
            ApothecaryWins = x.ApothecaryWins,
            ApothecaryLosses = x.ApothecaryLosses,
            ApothecaryKills = x.ApothecaryKills,
            ApothecaryDeaths = x.ApothecaryDeaths,
            ConjurerWins = x.ConjurerWins,
            ConjurerLosses = x.ConjurerLosses,
            ConjurerKills = x.ConjurerKills,
            ConjurerDeaths = x.ConjurerDeaths,
            SentinelWins = x.SentinelWins,
            SentinelLosses = x.SentinelLosses,
            SentinelKills = x.SentinelKills,
            SentinelDeaths = x.SentinelDeaths,
            LuminaryWins = x.LuminaryWins,
            LuminaryLosses = x.LuminaryLosses,
            LuminaryKills = x.LuminaryKills,
            LuminaryDeaths = x.LuminaryDeaths
        };

    private static ExperimentalSpecsWlWeekly ToWeeklySnapshot(ExperimentalSpecsWl x, int weekStartDate) =>
        new()
        {
            Uuid = x.Uuid,
            WeekStartDate = weekStartDate,
            PyromancerWins = x.PyromancerWins,
            PyromancerLosses = x.PyromancerLosses,
            PyromancerKills = x.PyromancerKills,
            PyromancerDeaths = x.PyromancerDeaths,
            CryomancerWins = x.CryomancerWins,
            CryomancerLosses = x.CryomancerLosses,
            CryomancerKills = x.CryomancerKills,
            CryomancerDeaths = x.CryomancerDeaths,
            AquamancerWins = x.AquamancerWins,
            AquamancerLosses = x.AquamancerLosses,
            AquamancerKills = x.AquamancerKills,
            AquamancerDeaths = x.AquamancerDeaths,
            BerserkerWins = x.BerserkerWins,
            BerserkerLosses = x.BerserkerLosses,
            BerserkerKills = x.BerserkerKills,
            BerserkerDeaths = x.BerserkerDeaths,
            DefenderWins = x.DefenderWins,
            DefenderLosses = x.DefenderLosses,
            DefenderKills = x.DefenderKills,
            DefenderDeaths = x.DefenderDeaths,
            RevenantWins = x.RevenantWins,
            RevenantLosses = x.RevenantLosses,
            RevenantKills = x.RevenantKills,
            RevenantDeaths = x.RevenantDeaths,
            AvengerWins = x.AvengerWins,
            AvengerLosses = x.AvengerLosses,
            AvengerKills = x.AvengerKills,
            AvengerDeaths = x.AvengerDeaths,
            CrusaderWins = x.CrusaderWins,
            CrusaderLosses = x.CrusaderLosses,
            CrusaderKills = x.CrusaderKills,
            CrusaderDeaths = x.CrusaderDeaths,
            ProtectorWins = x.ProtectorWins,
            ProtectorLosses = x.ProtectorLosses,
            ProtectorKills = x.ProtectorKills,
            ProtectorDeaths = x.ProtectorDeaths,
            ThunderlordWins = x.ThunderlordWins,
            ThunderlordLosses = x.ThunderlordLosses,
            ThunderlordKills = x.ThunderlordKills,
            ThunderlordDeaths = x.ThunderlordDeaths,
            SpiritguardWins = x.SpiritguardWins,
            SpiritguardLosses = x.SpiritguardLosses,
            SpiritguardKills = x.SpiritguardKills,
            SpiritguardDeaths = x.SpiritguardDeaths,
            EarthwardenWins = x.EarthwardenWins,
            EarthwardenLosses = x.EarthwardenLosses,
            EarthwardenKills = x.EarthwardenKills,
            EarthwardenDeaths = x.EarthwardenDeaths,
            AssassinWins = x.AssassinWins,
            AssassinLosses = x.AssassinLosses,
            AssassinKills = x.AssassinKills,
            AssassinDeaths = x.AssassinDeaths,
            VindicatorWins = x.VindicatorWins,
            VindicatorLosses = x.VindicatorLosses,
            VindicatorKills = x.VindicatorKills,
            VindicatorDeaths = x.VindicatorDeaths,
            ApothecaryWins = x.ApothecaryWins,
            ApothecaryLosses = x.ApothecaryLosses,
            ApothecaryKills = x.ApothecaryKills,
            ApothecaryDeaths = x.ApothecaryDeaths,
            ConjurerWins = x.ConjurerWins,
            ConjurerLosses = x.ConjurerLosses,
            ConjurerKills = x.ConjurerKills,
            ConjurerDeaths = x.ConjurerDeaths,
            SentinelWins = x.SentinelWins,
            SentinelLosses = x.SentinelLosses,
            SentinelKills = x.SentinelKills,
            SentinelDeaths = x.SentinelDeaths,
            LuminaryWins = x.LuminaryWins,
            LuminaryLosses = x.LuminaryLosses,
            LuminaryKills = x.LuminaryKills,
            LuminaryDeaths = x.LuminaryDeaths
        };
}
