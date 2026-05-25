using System.Text.Json;
using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Business.Services;

public interface IPlayerUuidUpdateService
{
    Task<PlayerUuidUpdateServiceResult> UpdateAsync(Guid oldUuid, Guid newUuid, CancellationToken cancellationToken);
}

public sealed class PlayerUuidUpdateService(
    IDbContextFactory<BalancerDbContext> dbContextFactory,
    IMinecraftPlayerResolveService minecraftPlayerResolveService) : IPlayerUuidUpdateService
{
    public async Task<PlayerUuidUpdateServiceResult> UpdateAsync(
        Guid oldUuid,
        Guid newUuid,
        CancellationToken cancellationToken)
    {
        if (oldUuid == newUuid)
        {
            return PlayerUuidUpdateServiceResult.Fail(400, "oldUuid and newUuid must be different.");
        }

        var resolved = await minecraftPlayerResolveService.ResolveAsync(newUuid.ToString(), cancellationToken);
        if (!resolved.Success)
        {
            return PlayerUuidUpdateServiceResult.Fail(
                resolved.StatusCode,
                resolved.Message ?? "Unable to resolve player.");
        }

        var displayName = resolved.Name.Trim();
        var tablesUpdated = new List<string>();

        await using var strategyDb = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var strategy = strategyDb.Database.CreateExecutionStrategy();

        var payload = await strategy.ExecuteAsync(async () =>
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            if (!await dbContext.BaseWeights.AnyAsync(x => x.Uuid == oldUuid, cancellationToken))
            {
                return (Result: PlayerUuidUpdateServiceResult.Fail(404, "Player not found in base_weights."), Commit: false);
            }

            if (await dbContext.BaseWeights.AnyAsync(x => x.Uuid == newUuid, cancellationToken))
            {
                return (Result: PlayerUuidUpdateServiceResult.Fail(
                    409,
                    "Player already exists in base_weights for newUuid."), Commit: false);
            }

            if (await HasCompositeKeyOverlapAsync(dbContext, oldUuid, newUuid, cancellationToken))
            {
                return (Result: PlayerUuidUpdateServiceResult.Fail(
                    409,
                    "newUuid already has snapshot rows for the same day or week as oldUuid."), Commit: false);
            }

            var oldNameRow = await dbContext.Names.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Uuid == oldUuid, cancellationToken);
            var mergedPreviousNames = oldNameRow?.PreviousNames ?? [];

            await UpdateUuidOnSetAsync(
                dbContext,
                dbContext.BaseWeightsDaily,
                "base_weights_daily",
                oldUuid,
                newUuid,
                tablesUpdated,
                cancellationToken);
            await UpdateUuidOnSetAsync(
                dbContext,
                dbContext.BaseWeightsWeekly,
                "base_weights_weekly",
                oldUuid,
                newUuid,
                tablesUpdated,
                cancellationToken);
            await UpdateUuidOnSetAsync(
                dbContext,
                dbContext.ExperimentalSpecWeightsWeekly,
                "experimental_spec_weights_weekly",
                oldUuid,
                newUuid,
                tablesUpdated,
                cancellationToken);
            await UpdateUuidOnSetAsync(
                dbContext,
                dbContext.ExperimentalSpecsWlDaily,
                "experimental_specs_wl_daily",
                oldUuid,
                newUuid,
                tablesUpdated,
                cancellationToken);
            await UpdateUuidOnSetAsync(
                dbContext,
                dbContext.ExperimentalSpecsWlWeekly,
                "experimental_specs_wl_weekly",
                oldUuid,
                newUuid,
                tablesUpdated,
                cancellationToken);
            await UpdateUuidOnSetAsync(
                dbContext,
                dbContext.AdjustmentDaily,
                "adjustment_daily",
                oldUuid,
                newUuid,
                tablesUpdated,
                cancellationToken);
            await UpdateUuidOnSetAsync(
                dbContext,
                dbContext.ExperimentalSpecBans,
                "experimental_spec_bans",
                oldUuid,
                newUuid,
                tablesUpdated,
                cancellationToken);
            await UpdateUuidOnSetAsync(
                dbContext,
                dbContext.ExperimentalSpecRequests,
                "experimental_spec_requests",
                oldUuid,
                newUuid,
                tablesUpdated,
                cancellationToken);
            await UpdateUuidOnSetAsync(
                dbContext,
                dbContext.ExperimentalSpecWeights,
                "experimental_spec_weights",
                oldUuid,
                newUuid,
                tablesUpdated,
                cancellationToken);
            await UpdateUuidOnSetAsync(
                dbContext,
                dbContext.ExperimentalSpecsWl,
                "experimental_specs_wl",
                oldUuid,
                newUuid,
                tablesUpdated,
                cancellationToken);
            await UpdateUuidOnSetAsync(
                dbContext,
                dbContext.BaseWeights,
                "base_weights",
                oldUuid,
                newUuid,
                tablesUpdated,
                cancellationToken);

            await UpdateUuidOnSetAsync(
                dbContext,
                dbContext.AdjustmentDailyLogs,
                "adjustment_daily_log",
                oldUuid,
                newUuid,
                tablesUpdated,
                cancellationToken);
            await UpdateUuidOnSetAsync(
                dbContext,
                dbContext.AdjustmentWeeklyLogs,
                "adjustment_weekly_log",
                oldUuid,
                newUuid,
                tablesUpdated,
                cancellationToken);
            await UpdateUuidOnSetAsync(
                dbContext,
                dbContext.AdjustmentManualDailyLogs,
                "adjustment_manual_daily_log",
                oldUuid,
                newUuid,
                tablesUpdated,
                cancellationToken);
            await UpdateUuidOnSetAsync(
                dbContext,
                dbContext.AdjustmentManualWeeklyLogs,
                "adjustment_manual_weekly_log",
                oldUuid,
                newUuid,
                tablesUpdated,
                cancellationToken);

            if (await UpdateExperimentalSpecLogsAsync(dbContext, oldUuid, newUuid, cancellationToken))
            {
                tablesUpdated.Add("experimental_spec_logs");
            }

            if (await ReassignNameAsync(
                    dbContext,
                    oldUuid,
                    newUuid,
                    displayName,
                    oldNameRow?.Name,
                    mergedPreviousNames,
                    cancellationToken))
            {
                tablesUpdated.Add("names");
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return (Result: PlayerUuidUpdateServiceResult.Ok(new PlayerUuidUpdatePayload(
                displayName,
                oldUuid,
                newUuid,
                [.. tablesUpdated])), Commit: true);
        });

        return payload.Result;
    }

    private static async Task<bool> HasCompositeKeyOverlapAsync(
        BalancerDbContext dbContext,
        Guid oldUuid,
        Guid newUuid,
        CancellationToken cancellationToken)
    {
        var oldDayDates = await dbContext.BaseWeightsDaily
            .Where(x => x.Uuid == oldUuid)
            .Select(x => x.DayStartDate)
            .ToListAsync(cancellationToken);
        if (oldDayDates.Count > 0 &&
            await dbContext.BaseWeightsDaily.AnyAsync(
                x => x.Uuid == newUuid && oldDayDates.Contains(x.DayStartDate),
                cancellationToken))
        {
            return true;
        }

        var oldWeekDates = await dbContext.BaseWeightsWeekly
            .Where(x => x.Uuid == oldUuid)
            .Select(x => x.WeekStartDate)
            .ToListAsync(cancellationToken);
        if (oldWeekDates.Count > 0 &&
            await dbContext.BaseWeightsWeekly.AnyAsync(
                x => x.Uuid == newUuid && oldWeekDates.Contains(x.WeekStartDate),
                cancellationToken))
        {
            return true;
        }

        var oldSpecWeightWeeks = await dbContext.ExperimentalSpecWeightsWeekly
            .Where(x => x.Uuid == oldUuid)
            .Select(x => x.WeekStartDate)
            .ToListAsync(cancellationToken);
        if (oldSpecWeightWeeks.Count > 0 &&
            await dbContext.ExperimentalSpecWeightsWeekly.AnyAsync(
                x => x.Uuid == newUuid && oldSpecWeightWeeks.Contains(x.WeekStartDate),
                cancellationToken))
        {
            return true;
        }

        var oldWlDayDates = await dbContext.ExperimentalSpecsWlDaily
            .Where(x => x.Uuid == oldUuid)
            .Select(x => x.DayStartDate)
            .ToListAsync(cancellationToken);
        if (oldWlDayDates.Count > 0 &&
            await dbContext.ExperimentalSpecsWlDaily.AnyAsync(
                x => x.Uuid == newUuid && oldWlDayDates.Contains(x.DayStartDate),
                cancellationToken))
        {
            return true;
        }

        var oldWlWeekDates = await dbContext.ExperimentalSpecsWlWeekly
            .Where(x => x.Uuid == oldUuid)
            .Select(x => x.WeekStartDate)
            .ToListAsync(cancellationToken);
        return oldWlWeekDates.Count > 0 &&
               await dbContext.ExperimentalSpecsWlWeekly.AnyAsync(
                   x => x.Uuid == newUuid && oldWlWeekDates.Contains(x.WeekStartDate),
                   cancellationToken);
    }

    private static async Task UpdateUuidOnSetAsync<TEntity>(
        BalancerDbContext dbContext,
        DbSet<TEntity> set,
        string tableName,
        Guid oldUuid,
        Guid newUuid,
        List<string> tablesUpdated,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var rowsUpdated = dbContext.Database.IsRelational()
            ? await UpdateUuidViaSqlAsync(dbContext, tableName, oldUuid, newUuid, cancellationToken)
            : await ReassignUuidViaCloneAsync(set, oldUuid, newUuid, cancellationToken);
        if (rowsUpdated == 0)
        {
            return;
        }

        tablesUpdated.Add(tableName);
    }

    private static async Task<int> ReassignUuidViaCloneAsync<TEntity>(
        DbSet<TEntity> set,
        Guid oldUuid,
        Guid newUuid,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var rows = await set.Where(e => EF.Property<Guid>(e, "Uuid") == oldUuid).ToListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            return 0;
        }

        set.RemoveRange(rows);
        foreach (var row in rows)
        {
            set.Add(CloneWithNewUuid(row, newUuid));
        }

        return rows.Count;
    }

    private static TEntity CloneWithNewUuid<TEntity>(TEntity source, Guid newUuid)
        where TEntity : class
    {
        var clone = JsonSerializer.Deserialize<TEntity>(JsonSerializer.Serialize(source))!;
        typeof(TEntity).GetProperty("Uuid")!.SetValue(clone, newUuid);
        return clone;
    }

    private static Task<int> UpdateUuidViaSqlAsync(
        BalancerDbContext dbContext,
        string tableName,
        Guid oldUuid,
        Guid newUuid,
        CancellationToken cancellationToken) =>
        tableName switch
        {
            "names" => dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE names SET uuid = {newUuid} WHERE uuid = {oldUuid}",
                cancellationToken),
            "base_weights" => dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE base_weights SET uuid = {newUuid} WHERE uuid = {oldUuid}",
                cancellationToken),
            "adjustment_daily" => dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE adjustment_daily SET uuid = {newUuid} WHERE uuid = {oldUuid}",
                cancellationToken),
            "experimental_spec_bans" => dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE experimental_spec_bans SET uuid = {newUuid} WHERE uuid = {oldUuid}",
                cancellationToken),
            "experimental_spec_weights" => dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE experimental_spec_weights SET uuid = {newUuid} WHERE uuid = {oldUuid}",
                cancellationToken),
            "experimental_specs_wl" => dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE experimental_specs_wl SET uuid = {newUuid} WHERE uuid = {oldUuid}",
                cancellationToken),
            "base_weights_daily" => dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE base_weights_daily SET uuid = {newUuid} WHERE uuid = {oldUuid}",
                cancellationToken),
            "base_weights_weekly" => dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE base_weights_weekly SET uuid = {newUuid} WHERE uuid = {oldUuid}",
                cancellationToken),
            "experimental_spec_weights_weekly" => dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE experimental_spec_weights_weekly SET uuid = {newUuid} WHERE uuid = {oldUuid}",
                cancellationToken),
            "experimental_specs_wl_daily" => dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE experimental_specs_wl_daily SET uuid = {newUuid} WHERE uuid = {oldUuid}",
                cancellationToken),
            "experimental_specs_wl_weekly" => dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE experimental_specs_wl_weekly SET uuid = {newUuid} WHERE uuid = {oldUuid}",
                cancellationToken),
            "adjustment_daily_log" => dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE adjustment_daily_log SET uuid = {newUuid} WHERE uuid = {oldUuid}",
                cancellationToken),
            "adjustment_weekly_log" => dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE adjustment_weekly_log SET uuid = {newUuid} WHERE uuid = {oldUuid}",
                cancellationToken),
            "adjustment_manual_daily_log" => dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE adjustment_manual_daily_log SET uuid = {newUuid} WHERE uuid = {oldUuid}",
                cancellationToken),
            "adjustment_manual_weekly_log" => dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE adjustment_manual_weekly_log SET uuid = {newUuid} WHERE uuid = {oldUuid}",
                cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(tableName), tableName, "Unsupported uuid table.")
        };

    private static async Task<bool> UpdateExperimentalSpecLogsAsync(
        BalancerDbContext dbContext,
        Guid oldUuid,
        Guid newUuid,
        CancellationToken cancellationToken)
    {
        var logs = await dbContext.ExperimentalSpecLogs
            .Where(r =>
                r.Pyromancer == oldUuid ||
                r.Cryomancer == oldUuid ||
                r.Aquamancer == oldUuid ||
                r.Berserker == oldUuid ||
                r.Defender == oldUuid ||
                r.Revenant == oldUuid ||
                r.Avenger == oldUuid ||
                r.Crusader == oldUuid ||
                r.Protector == oldUuid ||
                r.Thunderlord == oldUuid ||
                r.Spiritguard == oldUuid ||
                r.Earthwarden == oldUuid ||
                r.Assassin == oldUuid ||
                r.Vindicator == oldUuid ||
                r.Apothecary == oldUuid ||
                r.Conjurer == oldUuid ||
                r.Sentinel == oldUuid ||
                r.Luminary == oldUuid)
            .ToListAsync(cancellationToken);

        if (logs.Count == 0)
        {
            return false;
        }

        foreach (var row in logs)
        {
            ReassignSpecColumns(row, oldUuid, newUuid);
        }

        return true;
    }

    private static void ReassignSpecColumns(ExperimentalSpecLog row, Guid oldUuid, Guid newUuid)
    {
        if (row.Pyromancer == oldUuid) row.Pyromancer = newUuid;
        if (row.Cryomancer == oldUuid) row.Cryomancer = newUuid;
        if (row.Aquamancer == oldUuid) row.Aquamancer = newUuid;
        if (row.Berserker == oldUuid) row.Berserker = newUuid;
        if (row.Defender == oldUuid) row.Defender = newUuid;
        if (row.Revenant == oldUuid) row.Revenant = newUuid;
        if (row.Avenger == oldUuid) row.Avenger = newUuid;
        if (row.Crusader == oldUuid) row.Crusader = newUuid;
        if (row.Protector == oldUuid) row.Protector = newUuid;
        if (row.Thunderlord == oldUuid) row.Thunderlord = newUuid;
        if (row.Spiritguard == oldUuid) row.Spiritguard = newUuid;
        if (row.Earthwarden == oldUuid) row.Earthwarden = newUuid;
        if (row.Assassin == oldUuid) row.Assassin = newUuid;
        if (row.Vindicator == oldUuid) row.Vindicator = newUuid;
        if (row.Apothecary == oldUuid) row.Apothecary = newUuid;
        if (row.Conjurer == oldUuid) row.Conjurer = newUuid;
        if (row.Sentinel == oldUuid) row.Sentinel = newUuid;
        if (row.Luminary == oldUuid) row.Luminary = newUuid;
    }

    private static async Task<bool> ReassignNameAsync(
        BalancerDbContext dbContext,
        Guid oldUuid,
        Guid newUuid,
        string displayName,
        string? previousDisplayName,
        string[] mergedPreviousNames,
        CancellationToken cancellationToken)
    {
        var nameRow = await dbContext.Names.FirstOrDefaultAsync(x => x.Uuid == oldUuid, cancellationToken);
        if (nameRow is null)
        {
            return false;
        }

        var previousNames = mergedPreviousNames
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToList();

        if (!string.IsNullOrWhiteSpace(previousDisplayName) &&
            !string.Equals(previousDisplayName, displayName, StringComparison.OrdinalIgnoreCase) &&
            !previousNames.Any(n => string.Equals(n, previousDisplayName, StringComparison.OrdinalIgnoreCase)))
        {
            previousNames.Add(previousDisplayName);
        }

        if (!string.Equals(nameRow.Name, displayName, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(nameRow.Name) &&
            !previousNames.Any(n => string.Equals(n, nameRow.Name, StringComparison.OrdinalIgnoreCase)))
        {
            previousNames.Add(nameRow.Name);
        }

        var merged = previousNames
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (dbContext.Database.IsRelational())
        {
            var rowsUpdated = await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE names SET uuid = {newUuid} WHERE uuid = {oldUuid}",
                cancellationToken);
            if (rowsUpdated == 0)
            {
                return false;
            }

            dbContext.ChangeTracker.Clear();
            var updatedName = await dbContext.Names.FirstAsync(x => x.Uuid == newUuid, cancellationToken);
            updatedName.Name = displayName;
            updatedName.PreviousNames = merged;
            return true;
        }

        dbContext.Names.Remove(nameRow);
        dbContext.Names.Add(new PlayerName
        {
            Uuid = newUuid,
            Name = displayName,
            PreviousNames = merged
        });
        return true;
    }
}

public sealed record PlayerUuidUpdatePayload(
    string Name,
    Guid OldUuid,
    Guid NewUuid,
    string[] TablesUpdated);

public sealed record PlayerUuidUpdateServiceResult(
    bool Success,
    int StatusCode,
    string? Message,
    PlayerUuidUpdatePayload? Response)
{
    public static PlayerUuidUpdateServiceResult Ok(PlayerUuidUpdatePayload response) =>
        new(true, 200, null, response);

    public static PlayerUuidUpdateServiceResult Fail(int statusCode, string message) =>
        new(false, statusCode, message, null);
}
