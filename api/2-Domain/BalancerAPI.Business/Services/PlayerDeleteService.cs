using BalancerAPI.Data.Data;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Business.Services;

public interface IPlayerDeleteService
{
    Task<PlayerDeleteServiceResult> DeleteAsync(Guid uuid, CancellationToken cancellationToken);
}

public sealed class PlayerDeleteService(IDbContextFactory<BalancerDbContext> dbContextFactory) : IPlayerDeleteService
{
    public async Task<PlayerDeleteServiceResult> DeleteAsync(Guid uuid, CancellationToken cancellationToken)
    {
        var removedTables = new List<string>();
        var data = new Dictionary<string, object>(StringComparer.Ordinal);

        await using var strategyDb = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var strategy = strategyDb.Database.CreateExecutionStrategy();

        var payload = await strategy.ExecuteAsync(async () =>
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            if (!await dbContext.BaseWeights.AnyAsync(x => x.Uuid == uuid, cancellationToken))
            {
                return (Result: PlayerDeleteServiceResult.Fail(404, "Player not found in base_weights."), Commit: false);
            }

            var nameRow = await dbContext.Names.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Uuid == uuid, cancellationToken);
            var displayName = nameRow?.Name ?? uuid.ToString();

            await DeleteByUuidAsync(dbContext.BaseWeightsDaily, "base_weights_daily", uuid, removedTables, data, cancellationToken);
            await DeleteByUuidAsync(dbContext.BaseWeightsWeekly, "base_weights_weekly", uuid, removedTables, data, cancellationToken);
            await DeleteByUuidAsync(
                dbContext.ExperimentalSpecWeightsWeekly,
                "experimental_spec_weights_weekly",
                uuid,
                removedTables,
                data,
                cancellationToken);
            await DeleteByUuidAsync(
                dbContext.ExperimentalSpecsWlDaily,
                "experimental_specs_wl_daily",
                uuid,
                removedTables,
                data,
                cancellationToken);
            await DeleteByUuidAsync(
                dbContext.ExperimentalSpecsWlWeekly,
                "experimental_specs_wl_weekly",
                uuid,
                removedTables,
                data,
                cancellationToken);
            await DeleteByUuidAsync(dbContext.AdjustmentDaily, "adjustment_daily", uuid, removedTables, data, cancellationToken);
            await DeleteByUuidAsync(dbContext.ExperimentalSpecBans, "experimental_spec_bans", uuid, removedTables, data, cancellationToken);
            await DeleteByUuidAsync(
                dbContext.ExperimentalSpecWeights,
                "experimental_spec_weights",
                uuid,
                removedTables,
                data,
                cancellationToken);
            await DeleteByUuidAsync(dbContext.ExperimentalSpecsWl, "experimental_specs_wl", uuid, removedTables, data, cancellationToken);
            await DeleteByUuidAsync(dbContext.BaseWeights, "base_weights", uuid, removedTables, data, cancellationToken);
            await DeleteByUuidAsync(dbContext.Names, "names", uuid, removedTables, data, cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return (Result: PlayerDeleteServiceResult.Ok(new PlayerDeletePayload(
                displayName,
                uuid,
                [.. removedTables],
                data)), Commit: true);
        });

        return payload.Result;
    }

    private static async Task DeleteByUuidAsync<TEntity>(
        DbSet<TEntity> set,
        string tableName,
        Guid uuid,
        List<string> removedTables,
        Dictionary<string, object> data,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var rows = await set.Where(e => EF.Property<Guid>(e, "Uuid") == uuid).ToListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            return;
        }

        set.RemoveRange(rows);
        removedTables.Add(tableName);
        data[tableName] = rows;
    }
}

public sealed record PlayerDeletePayload(
    string Name,
    Guid Uuid,
    string[] TablesRemoved,
    IReadOnlyDictionary<string, object> Data);

public sealed record PlayerDeleteServiceResult(
    bool Success,
    int StatusCode,
    string? Message,
    PlayerDeletePayload? Response)
{
    public static PlayerDeleteServiceResult Ok(PlayerDeletePayload response) =>
        new(true, 200, null, response);

    public static PlayerDeleteServiceResult Fail(int statusCode, string message) =>
        new(false, statusCode, message, null);
}
