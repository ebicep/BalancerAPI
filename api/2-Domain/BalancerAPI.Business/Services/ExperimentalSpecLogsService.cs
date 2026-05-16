using BalancerAPI.Data.Data;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Business.Services;

public sealed class ExperimentalSpecLogsService(IDbContextFactory<BalancerDbContext> dbContextFactory)
    : IExperimentalSpecLogsService
{
    public async Task<ExperimentalSpecLogsResult> GetAllAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var rows = await db.ExperimentalSpecLogs
            .AsNoTracking()
            .Where(spec => spec.BalanceId != null)
            .Join(
                db.ExperimentalBalanceLogs.AsNoTracking(),
                spec => spec.BalanceId,
                balance => balance.BalanceId,
                (spec, balance) => new { spec, balance })
            .OrderBy(x => x.balance.CreatedAt)
            .ThenBy(x => x.balance.BalanceId)
            .Select(x => x.spec)
            .ToListAsync(cancellationToken);

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
