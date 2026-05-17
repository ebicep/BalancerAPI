using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Business.Services;

public interface IPlayerGetService
{
    Task<PlayerGetServiceResult> GetAsync(Guid uuid, CancellationToken cancellationToken);
}

public sealed class PlayerGetService(IDbContextFactory<BalancerDbContext> dbContextFactory) : IPlayerGetService
{
    public async Task<PlayerGetServiceResult> GetAsync(Guid uuid, CancellationToken cancellationToken)
    {
        var namesTask = LoadByUuidAsync(dbContextFactory, "names", uuid, db => db.Names, cancellationToken);
        var baseWeightsTask = LoadByUuidAsync(dbContextFactory, "base_weights", uuid, db => db.BaseWeights, cancellationToken);
        var baseWeightsDailyTask = LoadByUuidAsync(
            dbContextFactory,
            "base_weights_daily",
            uuid,
            db => db.BaseWeightsDaily,
            cancellationToken);
        var baseWeightsWeeklyTask = LoadByUuidAsync(
            dbContextFactory,
            "base_weights_weekly",
            uuid,
            db => db.BaseWeightsWeekly,
            cancellationToken);
        var experimentalSpecWeightsTask = LoadByUuidAsync(
            dbContextFactory,
            "experimental_spec_weights",
            uuid,
            db => db.ExperimentalSpecWeights,
            cancellationToken);
        var experimentalSpecWeightsWeeklyTask = LoadByUuidAsync(
            dbContextFactory,
            "experimental_spec_weights_weekly",
            uuid,
            db => db.ExperimentalSpecWeightsWeekly,
            cancellationToken);
        var experimentalSpecBansTask = LoadByUuidAsync(
            dbContextFactory,
            "experimental_spec_bans",
            uuid,
            db => db.ExperimentalSpecBans,
            cancellationToken);
        var experimentalSpecsWlTask = LoadByUuidAsync(
            dbContextFactory,
            "experimental_specs_wl",
            uuid,
            db => db.ExperimentalSpecsWl,
            cancellationToken);
        var experimentalSpecsWlDailyTask = LoadByUuidAsync(
            dbContextFactory,
            "experimental_specs_wl_daily",
            uuid,
            db => db.ExperimentalSpecsWlDaily,
            cancellationToken);
        var experimentalSpecsWlWeeklyTask = LoadByUuidAsync(
            dbContextFactory,
            "experimental_specs_wl_weekly",
            uuid,
            db => db.ExperimentalSpecsWlWeekly,
            cancellationToken);
        var adjustmentDailyTask = LoadByUuidAsync(
            dbContextFactory,
            "adjustment_daily",
            uuid,
            db => db.AdjustmentDaily,
            cancellationToken);

        await Task.WhenAll(
            namesTask,
            baseWeightsTask,
            baseWeightsDailyTask,
            baseWeightsWeeklyTask,
            experimentalSpecWeightsTask,
            experimentalSpecWeightsWeeklyTask,
            experimentalSpecBansTask,
            experimentalSpecsWlTask,
            experimentalSpecsWlDailyTask,
            experimentalSpecsWlWeeklyTask,
            adjustmentDailyTask);

        var data = new Dictionary<string, object>(StringComparer.Ordinal);
        PlayerName? nameRow = null;

        MergeTable(await namesTask, data, ref nameRow);
        MergeTable(await baseWeightsTask, data);
        MergeTable(await baseWeightsDailyTask, data);
        MergeTable(await baseWeightsWeeklyTask, data);
        MergeTable(await experimentalSpecWeightsTask, data);
        MergeTable(await experimentalSpecWeightsWeeklyTask, data);
        MergeTable(await experimentalSpecBansTask, data);
        MergeTable(await experimentalSpecsWlTask, data);
        MergeTable(await experimentalSpecsWlDailyTask, data);
        MergeTable(await experimentalSpecsWlWeeklyTask, data);
        MergeTable(await adjustmentDailyTask, data);

        if (!data.ContainsKey("base_weights"))
        {
            return PlayerGetServiceResult.Fail(404, "Player not found in base_weights.");
        }

        var displayName = nameRow?.Name ?? uuid.ToString();
        return PlayerGetServiceResult.Ok(new PlayerGetPayload(displayName, uuid, data));
    }

    private static async Task<(string Table, List<TEntity> Rows)> LoadByUuidAsync<TEntity>(
        IDbContextFactory<BalancerDbContext> factory,
        string tableName,
        Guid uuid,
        Func<BalancerDbContext, DbSet<TEntity>> setSelector,
        CancellationToken ct)
        where TEntity : class
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var rows = await setSelector(db).AsNoTracking()
            .Where(e => EF.Property<Guid>(e, "Uuid") == uuid)
            .ToListAsync(ct);
        return (tableName, rows);
    }

    private static void MergeTable<TEntity>(
        (string Table, List<TEntity> Rows) loaded,
        Dictionary<string, object> data,
        ref PlayerName? nameRow)
        where TEntity : class
    {
        if (loaded.Rows.Count == 0)
        {
            return;
        }

        data[loaded.Table] = loaded.Rows;
        if (loaded.Table == "names" && loaded.Rows[0] is PlayerName firstName)
        {
            nameRow = firstName;
        }
    }

    private static void MergeTable<TEntity>(
        (string Table, List<TEntity> Rows) loaded,
        Dictionary<string, object> data)
        where TEntity : class
    {
        if (loaded.Rows.Count == 0)
        {
            return;
        }

        data[loaded.Table] = loaded.Rows;
    }
}

public sealed record PlayerGetPayload(
    string Name,
    Guid Uuid,
    IReadOnlyDictionary<string, object> Data);

public sealed record PlayerGetServiceResult(
    bool Success,
    int StatusCode,
    string? Message,
    PlayerGetPayload? Response)
{
    public static PlayerGetServiceResult Ok(PlayerGetPayload response) =>
        new(true, 200, null, response);

    public static PlayerGetServiceResult Fail(int statusCode, string message) =>
        new(false, statusCode, message, null);
}
