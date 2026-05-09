using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;
namespace BalancerAPI.Business.Services;

public interface IPlayerAddService
{
    Task<PlayerAddServiceResult> AddAsync(Guid uuid, int baseWeight, CancellationToken cancellationToken);
}

public sealed class PlayerAddService(
    IDbContextFactory<BalancerDbContext> dbContextFactory,
    IMinecraftPlayerResolveService minecraftPlayerResolveService) : IPlayerAddService
{
    public async Task<PlayerAddServiceResult> AddAsync(Guid uuid, int baseWeight, CancellationToken cancellationToken)
    {
        // Independent reads can run in parallel, but DbContext itself is not thread-safe.
        var currentDayTask = GetCurrentDayIdAsync(dbContextFactory, cancellationToken);
        var currentWeekTask = GetCurrentWeekIdAsync(dbContextFactory, cancellationToken);

        var resolved = await minecraftPlayerResolveService.ResolveAsync(uuid.ToString(), cancellationToken);
        if (!resolved.Success)
        {
            return PlayerAddServiceResult.Fail(resolved.StatusCode, resolved.Message ?? "Unable to resolve player.");
        }

        var displayName = resolved.Name.Trim();
        var addedTables = new List<string>();

        await using var strategyDb = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var strategy = strategyDb.Database.CreateExecutionStrategy();

        var payload = await strategy.ExecuteAsync(async () =>
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            if (await dbContext.BaseWeights.AnyAsync(x => x.Uuid == uuid, cancellationToken))
            {
                return (Result: PlayerAddServiceResult.Fail(409, "Player already exists in base_weights."), Commit: false);
            }

            await UpsertNameAsync(dbContext, uuid, displayName, addedTables, cancellationToken);
            await InsertPlayerCoreTablesAsync(dbContext, uuid, baseWeight, addedTables, cancellationToken);

            var currentDayId = await currentDayTask;
            var currentWeekId = await currentWeekTask;
            await InsertCurrentSnapshotTablesAsync(
                dbContext,
                uuid,
                baseWeight,
                addedTables,
                currentDayId,
                currentWeekId,
                cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return (Result: PlayerAddServiceResult.Ok(new PlayerAddPayload(
                displayName,
                uuid,
                [.. addedTables])), Commit: true);
        });

        return payload.Result;
    }

    private static async Task<int?> GetCurrentDayIdAsync(
        IDbContextFactory<BalancerDbContext> dbContextFactory,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.TimeDays.Select(x => (int?)x.Id).MaxAsync(cancellationToken);
    }

    private static async Task<int?> GetCurrentWeekIdAsync(
        IDbContextFactory<BalancerDbContext> dbContextFactory,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.TimeWeeks.Select(x => (int?)x.Id).MaxAsync(cancellationToken);
    }

    private async Task UpsertNameAsync(
        BalancerDbContext dbContext,
        Guid uuid,
        string displayName,
        List<string> addedTables,
        CancellationToken cancellationToken)
    {
        var existingName = await dbContext.Names.FirstOrDefaultAsync(x => x.Uuid == uuid, cancellationToken);
        if (existingName is null)
        {
            dbContext.Names.Add(new PlayerName
            {
                Uuid = uuid,
                Name = displayName,
                PreviousNames = []
            });
            addedTables.Add("names");
            return;
        }

        // updating name if current name doesnt match database stored name
        if (!string.Equals(existingName.Name, displayName, StringComparison.OrdinalIgnoreCase))
        {
            var previousNames = (existingName.PreviousNames ?? [])
                .Where(n => !string.Equals(n, existingName.Name, StringComparison.OrdinalIgnoreCase))
                .ToList();
            previousNames.Add(existingName.Name);

            existingName.PreviousNames = previousNames
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            existingName.Name = displayName;
        }
    }

    private async Task InsertPlayerCoreTablesAsync(
        BalancerDbContext dbContext,
        Guid uuid,
        int baseWeight,
        List<string> addedTables,
        CancellationToken cancellationToken)
    {
        dbContext.BaseWeights.Add(new BaseWeight
        {
            Uuid = uuid,
            Weight = baseWeight,
            LastPlayed = null
        });
        addedTables.Add("base_weights");

        if (!await dbContext.ExperimentalSpecWeights.AnyAsync(x => x.Uuid == uuid, cancellationToken))
        {
            dbContext.ExperimentalSpecWeights.Add(new ExperimentalSpecWeight { Uuid = uuid });
            addedTables.Add("experimental_spec_weights");
        }

        if (!await dbContext.ExperimentalSpecsWl.AnyAsync(x => x.Uuid == uuid, cancellationToken))
        {
            dbContext.ExperimentalSpecsWl.Add(new ExperimentalSpecsWl { Uuid = uuid });
            addedTables.Add("experimental_specs_wl");
        }

        if (!await dbContext.AdjustmentDaily.AnyAsync(x => x.Uuid == uuid, cancellationToken))
        {
            dbContext.AdjustmentDaily.Add(new AdjustmentDaily { Uuid = uuid, Trajectory = 0 });
            addedTables.Add("adjustment_daily");
        }
    }

    private async Task InsertCurrentSnapshotTablesAsync(
        BalancerDbContext dbContext,
        Guid uuid,
        int baseWeight,
        List<string> addedTables,
        int? currentDayId,
        int? currentWeekId,
        CancellationToken cancellationToken)
    {
        if (currentDayId is not null)
        {
            if (!await dbContext.BaseWeightsDaily.AnyAsync(
                    x => x.Uuid == uuid && x.DayStartDate == currentDayId.Value,
                    cancellationToken))
            {
                dbContext.BaseWeightsDaily.Add(new BaseWeightDaily
                {
                    Uuid = uuid,
                    DayStartDate = currentDayId.Value,
                    Weight = baseWeight
                });
                addedTables.Add("base_weights_daily");
            }

            if (!await dbContext.ExperimentalSpecsWlDaily.AnyAsync(
                    x => x.Uuid == uuid && x.DayStartDate == currentDayId.Value,
                    cancellationToken))
            {
                dbContext.ExperimentalSpecsWlDaily.Add(new ExperimentalSpecsWlDaily
                {
                    Uuid = uuid,
                    DayStartDate = currentDayId.Value
                });
                addedTables.Add("experimental_specs_wl_daily");
            }
        }

        if (currentWeekId is null)
        {
            return;
        }

        if (!await dbContext.BaseWeightsWeekly.AnyAsync(
                x => x.Uuid == uuid && x.WeekStartDate == currentWeekId.Value,
                cancellationToken))
        {
            dbContext.BaseWeightsWeekly.Add(new BaseWeightWeekly
            {
                Uuid = uuid,
                WeekStartDate = currentWeekId.Value,
                Weight = baseWeight
            });
            addedTables.Add("base_weights_weekly");
        }

        if (!await dbContext.ExperimentalSpecWeightsWeekly.AnyAsync(
                x => x.Uuid == uuid && x.WeekStartDate == currentWeekId.Value,
                cancellationToken))
        {
            dbContext.ExperimentalSpecWeightsWeekly.Add(new ExperimentalSpecWeightWeekly
            {
                Uuid = uuid,
                WeekStartDate = currentWeekId.Value
            });
            addedTables.Add("experimental_spec_weights_weekly");
        }

        if (!await dbContext.ExperimentalSpecsWlWeekly.AnyAsync(
                x => x.Uuid == uuid && x.WeekStartDate == currentWeekId.Value,
                cancellationToken))
        {
            dbContext.ExperimentalSpecsWlWeekly.Add(new ExperimentalSpecsWlWeekly
            {
                Uuid = uuid,
                WeekStartDate = currentWeekId.Value
            });
            addedTables.Add("experimental_specs_wl_weekly");
        }
    }

}

public sealed record PlayerAddPayload(
    string Name,
    Guid Uuid,
    string[] TablesAdded);

public sealed record PlayerAddServiceResult(
    bool Success,
    int StatusCode,
    string? Message,
    PlayerAddPayload? Response)
{
    public static PlayerAddServiceResult Ok(PlayerAddPayload response) =>
        new(true, 200, null, response);

    public static PlayerAddServiceResult Fail(int statusCode, string message) =>
        new(false, statusCode, message, null);
}
