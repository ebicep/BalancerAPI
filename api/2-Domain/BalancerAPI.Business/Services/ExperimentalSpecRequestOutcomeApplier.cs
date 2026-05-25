using BalancerAPI.Data.Data;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Business.Services;

internal static class ExperimentalSpecRequestOutcomeApplier
{
    public static async Task ApplyFromBalanceTeamsAsync(
        BalancerDbContext db,
        IReadOnlyList<ExperimentalBalanceTeam> teams,
        CancellationToken cancellationToken)
    {
        var allPlayers = teams.SelectMany(t => t.Specs).ToList();
        if (allPlayers.Count == 0)
        {
            return;
        }

        var uuids = allPlayers.Select(p => p.Uuid).ToList();
        var rows = await db.ExperimentalSpecRequests
            .Where(x => uuids.Contains(x.Uuid))
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return;
        }

        var specByUuid = allPlayers.ToDictionary(p => p.Uuid, p => p.Spec);

        foreach (var row in rows)
        {
            if (!specByUuid.TryGetValue(row.Uuid, out var assignedSpec))
            {
                continue;
            }

            if (string.Equals(assignedSpec, row.Spec, StringComparison.Ordinal))
            {
                db.ExperimentalSpecRequests.Remove(row);
            }
            else
            {
                row.GameCooldown--;
            }
        }
    }
}
