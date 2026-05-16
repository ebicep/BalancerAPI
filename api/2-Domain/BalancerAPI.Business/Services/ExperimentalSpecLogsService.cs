using System.Data;
using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Business.Services;

public sealed class ExperimentalSpecLogsService(IDbContextFactory<BalancerDbContext> dbContextFactory)
    : IExperimentalSpecLogsService
{
    public async Task<ExperimentalSpecLogsResult> GetAllAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await LoadOrderedRowsAsync(db, trackChanges: false, cancellationToken);
        return await BuildResponseAsync(rows, db, cancellationToken);
    }

    public async Task<ExperimentalSpecLogsResult> TruncateAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var rows = await LoadOrderedRowsAsync(db, trackChanges: true, cancellationToken);
        var removeCount = ComputeRemoveCount(rows.Count);
        var removed = rows.Take(removeCount).ToList();

        var buildResult = await BuildResponseAsync(removed, db, cancellationToken);
        if (!buildResult.Success)
        {
            await tx.RollbackAsync(cancellationToken);
            return buildResult;
        }

        if (removed.Count > 0)
        {
            db.ExperimentalSpecLogs.RemoveRange(removed);
            await db.SaveChangesAsync(cancellationToken);
        }

        await tx.CommitAsync(cancellationToken);
        return buildResult;
    }

    private static int ComputeRemoveCount(int total)
    {
        if (total <= 0)
        {
            return 0;
        }

        var n = (int)Math.Floor(total * 0.4);
        return n - (n % 2);
    }

    private static async Task<List<ExperimentalSpecLog>> LoadOrderedRowsAsync(
        BalancerDbContext db,
        bool trackChanges,
        CancellationToken cancellationToken)
    {
        var query = db.ExperimentalSpecLogs
            .Where(spec => spec.BalanceId != null)
            .Join(
                db.ExperimentalBalanceLogs,
                spec => spec.BalanceId,
                balance => balance.BalanceId,
                (spec, balance) => new { spec, balance })
            .OrderBy(x => x.balance.CreatedAt)
            .ThenBy(x => x.balance.BalanceId)
            .Select(x => x.spec);

        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return await query.ToListAsync(cancellationToken);
    }

    private static async Task<ExperimentalSpecLogsResult> BuildResponseAsync(
        IReadOnlyList<ExperimentalSpecLog> rows,
        BalancerDbContext db,
        CancellationToken cancellationToken)
    {
        var log = ExperimentalSpecs.AllOrdered.ToDictionary(
            spec => spec.ToLowerInvariant(),
            _ => new List<string>(),
            StringComparer.Ordinal);

        var uuidSet = new HashSet<Guid>();
        foreach (var row in rows)
        {
            foreach (var (_, uuid) in ExperimentalSpecLogColumns.EnumerateAssignments(row))
            {
                uuidSet.Add(uuid);
            }
        }

        if (uuidSet.Count > 0)
        {
            var names = await db.Names
                .AsNoTracking()
                .Where(n => uuidSet.Contains(n.Uuid))
                .ToDictionaryAsync(n => n.Uuid, n => n.Name, cancellationToken);

            if (names.Count != uuidSet.Count)
            {
                var missing = uuidSet.First(uuid => !names.ContainsKey(uuid));
                return new ExperimentalSpecLogsResult(
                    false,
                    500,
                    $"No name found for player {missing}.",
                    null);
            }

            foreach (var row in rows)
            {
                foreach (var (spec, uuid) in ExperimentalSpecLogColumns.EnumerateAssignments(row))
                {
                    log[spec.ToLowerInvariant()].Add(names[uuid]);
                }
            }
        }

        var readOnlyLog = log.ToDictionary(
            kv => kv.Key,
            kv => (IReadOnlyList<string>)kv.Value,
            StringComparer.Ordinal);

        return new ExperimentalSpecLogsResult(
            true,
            200,
            null,
            new ExperimentalSpecLogsResponse(rows.Count, readOnlyLog));
    }
}
