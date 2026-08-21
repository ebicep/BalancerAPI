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

    public async Task<ExperimentalSpecLogsResult> TruncateLastAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var rows = await LoadOrderedRowsAsync(db, trackChanges: true, cancellationToken);
        var removed = rows.TakeLast(2).ToList();

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

    public async Task<ExperimentalSpecLogsResult> ClearAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var rows = await LoadOrderedRowsAsync(db, trackChanges: true, cancellationToken);
        var removed = rows;

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

    public async Task<ExperimentalSpecLogsResult> UntruncateAsync(
        ExperimentalSpecLogsResponse? request,
        CancellationToken cancellationToken)
    {
        if (request?.Rows is null)
        {
            return new ExperimentalSpecLogsResult(
                false,
                400,
                "rows is required.",
                null);
        }

        var unique = new List<ExperimentalSpecLogRowSnapshot>();
        var seen = new HashSet<Guid>();
        foreach (var row in request.Rows)
        {
            if (row.Id is not { } id || id == Guid.Empty
                || row.BalanceId is not { } balanceId || balanceId == Guid.Empty)
            {
                return new ExperimentalSpecLogsResult(
                    false,
                    400,
                    "Each row must include id and balanceId.",
                    null);
            }

            if (seen.Add(id))
            {
                unique.Add(row);
            }
        }

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var ids = unique.Select(r => r.Id!.Value).ToList();
        var existingIds = ids.Count == 0
            ? []
            : await db.ExperimentalSpecLogs
                .AsNoTracking()
                .Where(x => ids.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
        var existingSet = existingIds.ToHashSet();
        var toInsert = unique.Where(r => !existingSet.Contains(r.Id!.Value)).ToList();

        if (toInsert.Count > 0)
        {
            var balanceIds = toInsert.Select(r => r.BalanceId!.Value).Distinct().ToList();
            var existingBalanceIds = await db.ExperimentalBalanceLogs
                .AsNoTracking()
                .Where(x => balanceIds.Contains(x.BalanceId))
                .Select(x => x.BalanceId)
                .ToListAsync(cancellationToken);
            var balanceSet = existingBalanceIds.ToHashSet();
            var missingBalanceId = balanceIds.FirstOrDefault(id => !balanceSet.Contains(id));
            if (missingBalanceId != Guid.Empty)
            {
                await tx.RollbackAsync(cancellationToken);
                return new ExperimentalSpecLogsResult(
                    false,
                    400,
                    $"No experimental balance log found for balance {missingBalanceId}.",
                    null);
            }
        }

        if (toInsert.Count > 0)
        {
            db.ExperimentalSpecLogs.AddRange(toInsert.Select(ToEntity));
            await db.SaveChangesAsync(cancellationToken);
        }

        var allRows = await LoadOrderedRowsAsync(db, trackChanges: false, cancellationToken);
        var buildResult = await BuildResponseAsync(allRows, db, cancellationToken, includeRows: false);
        if (!buildResult.Success)
        {
            await tx.RollbackAsync(cancellationToken);
            return buildResult;
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
        CancellationToken cancellationToken,
        bool includeRows = true)
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

        IReadOnlyList<ExperimentalSpecLogRowSnapshot>? snapshots = includeRows
            ? rows.Select(ToSnapshot).ToList()
            : null;
        return new ExperimentalSpecLogsResult(
            true,
            200,
            null,
            new ExperimentalSpecLogsResponse(rows.Count, readOnlyLog, snapshots));
    }

    private static ExperimentalSpecLogRowSnapshot ToSnapshot(ExperimentalSpecLog row) => new(
        row.Id,
        row.BalanceId,
        row.Pyromancer,
        row.Cryomancer,
        row.Aquamancer,
        row.Berserker,
        row.Defender,
        row.Revenant,
        row.Avenger,
        row.Crusader,
        row.Protector,
        row.Thunderlord,
        row.Spiritguard,
        row.Earthwarden,
        row.Assassin,
        row.Vindicator,
        row.Apothecary,
        row.Conjurer,
        row.Sentinel,
        row.Luminary);

    private static ExperimentalSpecLog ToEntity(ExperimentalSpecLogRowSnapshot row) => new()
    {
        Id = row.Id!.Value,
        BalanceId = row.BalanceId,
        Pyromancer = row.Pyromancer,
        Cryomancer = row.Cryomancer,
        Aquamancer = row.Aquamancer,
        Berserker = row.Berserker,
        Defender = row.Defender,
        Revenant = row.Revenant,
        Avenger = row.Avenger,
        Crusader = row.Crusader,
        Protector = row.Protector,
        Thunderlord = row.Thunderlord,
        Spiritguard = row.Spiritguard,
        Earthwarden = row.Earthwarden,
        Assassin = row.Assassin,
        Vindicator = row.Vindicator,
        Apothecary = row.Apothecary,
        Conjurer = row.Conjurer,
        Sentinel = row.Sentinel,
        Luminary = row.Luminary
    };
}
