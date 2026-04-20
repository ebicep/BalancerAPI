using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using System.Data;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Business.Services;

public sealed record NewDayResponse(int NewDay);

public sealed record NewWeekResponse(int NewWeek);

public sealed record NewSeasonResponse(int Season, DateTime Timestamp);

public sealed record LatestSeasonResponse(int Season, DateTime Timestamp);

public sealed class TimeService(BalancerDbContext dbContext, IDbContextFactory<BalancerDbContext> dbContextFactory)
    : ITimeService
{
    public async Task<int> CreateNewDayAsync(CancellationToken cancellationToken)
    {
        await using var tx = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var maxId = await dbContext.TimeDays
            .Select(x => (int?)x.Id)
            .MaxAsync(cancellationToken);

        var previousBoundary = await dbContext.TimeDays
            .OrderByDescending(x => x.Id)
            .Select(x => (DateTime?)x.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        var newId = (maxId ?? -1) + 1;
        var timestamp = EasternTime.Now;

        dbContext.TimeDays.Add(new TimeDay
        {
            Id = newId,
            Timestamp = timestamp
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        var adjustmentRows = await dbContext.AdjustmentDaily.ToListAsync(cancellationToken);
        if (adjustmentRows.Count > 0)
        {
            dbContext.AdjustmentDaily.RemoveRange(adjustmentRows);
        }

        var boundary = previousBoundary ?? DateTime.MinValue;

        var loadBaseTask = LoadChangedBaseWeightsDailyAsync(newId, boundary, cancellationToken);
        var loadWlTask = LoadChangedExperimentalSpecsWlDailyAsync(newId, boundary, cancellationToken);
        await Task.WhenAll(loadBaseTask, loadWlTask);

        var changedBaseWeights = await loadBaseTask;
        if (changedBaseWeights.Count > 0)
        {
            dbContext.BaseWeightsDaily.AddRange(changedBaseWeights);
        }

        var changedWl = await loadWlTask;
        if (changedWl.Count > 0)
        {
            dbContext.ExperimentalSpecsWlDaily.AddRange(changedWl);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return newId;
    }

    public async Task<int> CreateNewWeekAsync(CancellationToken cancellationToken)
    {
        await using var tx = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var maxId = await dbContext.TimeWeeks
            .Select(x => (int?)x.Id)
            .MaxAsync(cancellationToken);

        var previousBoundary = await dbContext.TimeWeeks
            .OrderByDescending(x => x.Id)
            .Select(x => (DateTime?)x.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        var newId = (maxId ?? -1) + 1;
        var timestamp = EasternTime.Now;

        dbContext.TimeWeeks.Add(new TimeWeek
        {
            Id = newId,
            Timestamp = timestamp
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        var boundary = previousBoundary ?? DateTime.MinValue;

        var loadBaseTask = LoadChangedBaseWeightsWeeklyAsync(newId, boundary, cancellationToken);
        var loadSpecWeightsTask = LoadChangedExperimentalSpecWeightsWeeklyAsync(newId, boundary, cancellationToken);
        var loadWlTask = LoadChangedExperimentalSpecsWlWeeklyAsync(newId, boundary, cancellationToken);
        await Task.WhenAll(loadBaseTask, loadSpecWeightsTask, loadWlTask);

        var changedBaseWeights = await loadBaseTask;
        if (changedBaseWeights.Count > 0)
        {
            dbContext.BaseWeightsWeekly.AddRange(changedBaseWeights);
        }

        var changedSpecWeights = await loadSpecWeightsTask;
        if (changedSpecWeights.Count > 0)
        {
            dbContext.ExperimentalSpecWeightsWeekly.AddRange(changedSpecWeights);
        }

        var changedWl = await loadWlTask;
        if (changedWl.Count > 0)
        {
            dbContext.ExperimentalSpecsWlWeekly.AddRange(changedWl);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return newId;
    }

    public async Task<bool> UndoDayAsync(int dayId, CancellationToken cancellationToken)
    {
        await using var tx = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var timeDay = await dbContext.TimeDays
            .SingleOrDefaultAsync(x => x.Id == dayId, cancellationToken);
        if (timeDay is null)
        {
            return false;
        }

        var baseWeightsDaily = await dbContext.BaseWeightsDaily
            .Where(x => x.DayStartDate == dayId)
            .ToListAsync(cancellationToken);
        if (baseWeightsDaily.Count > 0)
        {
            dbContext.BaseWeightsDaily.RemoveRange(baseWeightsDaily);
        }

        var specsWlDaily = await dbContext.ExperimentalSpecsWlDaily
            .Where(x => x.DayStartDate == dayId)
            .ToListAsync(cancellationToken);
        if (specsWlDaily.Count > 0)
        {
            dbContext.ExperimentalSpecsWlDaily.RemoveRange(specsWlDaily);
        }

        dbContext.TimeDays.Remove(timeDay);
        await dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UndoWeekAsync(int weekId, CancellationToken cancellationToken)
    {
        await using var tx = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var timeWeek = await dbContext.TimeWeeks
            .SingleOrDefaultAsync(x => x.Id == weekId, cancellationToken);
        if (timeWeek is null)
        {
            return false;
        }

        var baseWeightsWeekly = await dbContext.BaseWeightsWeekly
            .Where(x => x.WeekStartDate == weekId)
            .ToListAsync(cancellationToken);
        if (baseWeightsWeekly.Count > 0)
        {
            dbContext.BaseWeightsWeekly.RemoveRange(baseWeightsWeekly);
        }

        var specWeightsWeekly = await dbContext.ExperimentalSpecWeightsWeekly
            .Where(x => x.WeekStartDate == weekId)
            .ToListAsync(cancellationToken);
        if (specWeightsWeekly.Count > 0)
        {
            dbContext.ExperimentalSpecWeightsWeekly.RemoveRange(specWeightsWeekly);
        }

        var specsWlWeekly = await dbContext.ExperimentalSpecsWlWeekly
            .Where(x => x.WeekStartDate == weekId)
            .ToListAsync(cancellationToken);
        if (specsWlWeekly.Count > 0)
        {
            dbContext.ExperimentalSpecsWlWeekly.RemoveRange(specsWlWeekly);
        }

        dbContext.TimeWeeks.Remove(timeWeek);
        await dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }

    private async Task<List<BaseWeightDaily>> LoadChangedBaseWeightsDailyAsync(
        int newId,
        DateTime boundary,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var alreadySnapshottedUuids = await db.BaseWeightsDaily.AsNoTracking()
            .Where(x => x.DayStartDate == newId)
            .Select(x => x.Uuid)
            .ToHashSetAsync(cancellationToken);

        return await db.BaseWeights.AsNoTracking()
            .Where(x => x.LastUpdated > boundary && !alreadySnapshottedUuids.Contains(x.Uuid))
            .Select(x => new BaseWeightDaily
            {
                Uuid = x.Uuid,
                DayStartDate = newId,
                Weight = x.Weight
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<List<ExperimentalSpecsWlDaily>> LoadChangedExperimentalSpecsWlDailyAsync(
        int newId,
        DateTime boundary,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var alreadyWlSnapshottedUuids = await db.ExperimentalSpecsWlDaily.AsNoTracking()
            .Where(x => x.DayStartDate == newId)
            .Select(x => x.Uuid)
            .ToHashSetAsync(cancellationToken);

        return await db.ExperimentalSpecsWl.AsNoTracking()
            .Where(x => x.LastUpdated > boundary && !alreadyWlSnapshottedUuids.Contains(x.Uuid))
            .Select(x => new ExperimentalSpecsWlDaily
            {
                Uuid = x.Uuid,
                DayStartDate = newId,

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
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<List<BaseWeightWeekly>> LoadChangedBaseWeightsWeeklyAsync(
        int newId,
        DateTime boundary,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var alreadyBaseWeightSnapshottedUuids = await db.BaseWeightsWeekly.AsNoTracking()
            .Where(x => x.WeekStartDate == newId)
            .Select(x => x.Uuid)
            .ToHashSetAsync(cancellationToken);

        return await db.BaseWeights.AsNoTracking()
            .Where(x => x.LastUpdated > boundary && !alreadyBaseWeightSnapshottedUuids.Contains(x.Uuid))
            .Select(x => new BaseWeightWeekly
            {
                Uuid = x.Uuid,
                WeekStartDate = newId,
                Weight = x.Weight
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<List<ExperimentalSpecWeightWeekly>> LoadChangedExperimentalSpecWeightsWeeklyAsync(
        int newId,
        DateTime boundary,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var alreadySpecWeightSnapshottedUuids = await db.ExperimentalSpecWeightsWeekly.AsNoTracking()
            .Where(x => x.WeekStartDate == newId)
            .Select(x => x.Uuid)
            .ToHashSetAsync(cancellationToken);

        return await db.ExperimentalSpecWeights.AsNoTracking()
            .Where(x => x.LastUpdated > boundary && !alreadySpecWeightSnapshottedUuids.Contains(x.Uuid))
            .Select(x => new ExperimentalSpecWeightWeekly
            {
                Uuid = x.Uuid,
                WeekStartDate = newId,

                PyromancerOffset = x.PyromancerOffset,
                CryomancerOffset = x.CryomancerOffset,
                AquamancerOffset = x.AquamancerOffset,
                BerserkerOffset = x.BerserkerOffset,
                DefenderOffset = x.DefenderOffset,
                RevenantOffset = x.RevenantOffset,
                AvengerOffset = x.AvengerOffset,
                CrusaderOffset = x.CrusaderOffset,
                ProtectorOffset = x.ProtectorOffset,
                ThunderlordOffset = x.ThunderlordOffset,
                SpiritguardOffset = x.SpiritguardOffset,
                EarthwardenOffset = x.EarthwardenOffset,
                AssassinOffset = x.AssassinOffset,
                VindicatorOffset = x.VindicatorOffset,
                ApothecaryOffset = x.ApothecaryOffset,
                ConjurerOffset = x.ConjurerOffset,
                SentinelOffset = x.SentinelOffset,
                LuminaryOffset = x.LuminaryOffset
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<List<ExperimentalSpecsWlWeekly>> LoadChangedExperimentalSpecsWlWeeklyAsync(
        int newId,
        DateTime boundary,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var alreadyWlSnapshottedUuids = await db.ExperimentalSpecsWlWeekly.AsNoTracking()
            .Where(x => x.WeekStartDate == newId)
            .Select(x => x.Uuid)
            .ToHashSetAsync(cancellationToken);

        return await db.ExperimentalSpecsWl.AsNoTracking()
            .Where(x => x.LastUpdated > boundary && !alreadyWlSnapshottedUuids.Contains(x.Uuid))
            .Select(x => new ExperimentalSpecsWlWeekly
            {
                Uuid = x.Uuid,
                WeekStartDate = newId,

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
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<(int Season, DateTime Timestamp)> CreateNewSeasonAsync(CancellationToken cancellationToken)
    {
        var maxId = await dbContext.TimeSeasons
            .Select(x => (int?)x.Id)
            .MaxAsync(cancellationToken);

        var newId = (maxId ?? -1) + 1;
        var timestamp = EasternTime.Now;

        dbContext.TimeSeasons.Add(new TimeSeason
        {
            Id = newId,
            Timestamp = timestamp
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return (newId, timestamp);
    }

    public async Task<bool> UndoSeasonAsync(int seasonId, CancellationToken cancellationToken)
    {
        await using var tx = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var timeSeason = await dbContext.TimeSeasons
            .SingleOrDefaultAsync(x => x.Id == seasonId, cancellationToken);
        if (timeSeason is null)
        {
            return false;
        }

        dbContext.TimeSeasons.Remove(timeSeason);
        await dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<(int Season, DateTime Timestamp)?> GetLatestSeasonAsync(CancellationToken cancellationToken)
    {
        var latest = await dbContext.TimeSeasons
            .OrderByDescending(x => x.Id)
            .Select(x => new { x.Id, x.Timestamp })
            .FirstOrDefaultAsync(cancellationToken);

        return latest is null ? null : (latest.Id, latest.Timestamp);
    }
}

internal static class EasternTime
{
    private static readonly TimeZoneInfo Zone = ResolveEasternTimeZone();
    internal static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);

    private static TimeZoneInfo ResolveEasternTimeZone()
    {
        foreach (var id in new[] { "Eastern Standard Time", "America/New_York" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
                // try next id (Windows vs IANA)
            }
            catch (InvalidTimeZoneException)
            {
                // try next id
            }
        }

        throw new InvalidOperationException(
            "Could not resolve Eastern time zone. Tried 'Eastern Standard Time' and 'America/New_York'.");
    }
}