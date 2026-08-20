using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Business.Services;

public interface IExperimentalSpecBanService
{
    Task<ExperimentalSpecBanServiceResult> GetBansAsync(Guid uuid, CancellationToken cancellationToken);

    Task<ExperimentalSpecBanServiceResult> SetBanAsync(
        Guid uuid,
        string canonicalSpec,
        bool banned,
        CancellationToken cancellationToken);

    Task<ExperimentalSpecBanServiceResult> SetBansAsync(
        Guid uuid,
        IReadOnlyList<string> canonicalSpecs,
        bool banned,
        CancellationToken cancellationToken);
}

public sealed class ExperimentalSpecBanService(IDbContextFactory<BalancerDbContext> dbContextFactory)
    : IExperimentalSpecBanService
{
    public async Task<ExperimentalSpecBanServiceResult> GetBansAsync(Guid uuid, CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var row = await db.ExperimentalSpecBans
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Uuid == uuid, cancellationToken);

        return Ok(BansFromRow(row));
    }

    public Task<ExperimentalSpecBanServiceResult> SetBanAsync(
        Guid uuid,
        string canonicalSpec,
        bool banned,
        CancellationToken cancellationToken) =>
        SetBansAsync(uuid, [canonicalSpec], banned, cancellationToken);

    public async Task<ExperimentalSpecBanServiceResult> SetBansAsync(
        Guid uuid,
        IReadOnlyList<string> canonicalSpecs,
        bool banned,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var row = await db.ExperimentalSpecBans
            .FirstOrDefaultAsync(x => x.Uuid == uuid, cancellationToken);

        var alreadyInDesiredState = canonicalSpecs.All(spec =>
            banned
                ? row is not null && ExperimentalSpecBanFlags.GetBanFlag(row, spec)
                : row is null || !ExperimentalSpecBanFlags.GetBanFlag(row, spec));
        if (alreadyInDesiredState)
        {
            return Fail(400, GroupAlreadyInStateMessage(canonicalSpecs, banned));
        }

        if (row is null)
        {
            row = new ExperimentalSpecBan { Uuid = uuid };
            db.ExperimentalSpecBans.Add(row);
        }

        var newlyBanned = new List<string>();
        foreach (var spec in canonicalSpecs)
        {
            var currentlyBanned = ExperimentalSpecBanFlags.GetBanFlag(row, spec);
            if (currentlyBanned == banned)
            {
                continue;
            }

            SetBanFlag(row, spec, banned);
            if (banned)
            {
                newlyBanned.Add(spec);
            }
        }

        if (newlyBanned.Count > 0)
        {
            var matchingRequests = await db.ExperimentalSpecRequests
                .Where(x => x.Uuid == uuid && newlyBanned.Contains(x.Spec))
                .ToListAsync(cancellationToken);
            db.ExperimentalSpecRequests.RemoveRange(matchingRequests);
        }

        await db.SaveChangesAsync(cancellationToken);

        return Ok(BansFromRow(row));
    }

    private static ExperimentalSpecBanServiceResult Ok(IReadOnlyList<string> bans) =>
        new(true, 200, null, new ExperimentalSpecBansResponse(bans));

    private static ExperimentalSpecBanServiceResult Fail(int statusCode, string message) =>
        new(false, statusCode, message, null);

    private static string GroupAlreadyInStateMessage(IReadOnlyList<string> canonicalSpecs, bool banned)
    {
        if (canonicalSpecs.Count == 1)
        {
            return banned
                ? $"Player is already banned from {canonicalSpecs[0]}."
                : $"Player is not banned from {canonicalSpecs[0]}.";
        }

        return banned
            ? "Player is already banned from all requested specs."
            : "Player is not banned from any requested specs.";
    }

    private static IReadOnlyList<string> BansFromRow(ExperimentalSpecBan? row)
    {
        if (row is null)
        {
            return [];
        }

        var bans = new List<string>(ExperimentalSpecs.AllOrdered.Length);
        foreach (var spec in ExperimentalSpecs.AllOrdered)
        {
            if (ExperimentalSpecBanFlags.GetBanFlag(row, spec))
            {
                bans.Add(spec);
            }
        }

        return bans;
    }

    private static void SetBanFlag(ExperimentalSpecBan row, string spec, bool banned) =>
        ExperimentalSpecBanFlags.SetBanFlag(row, spec, banned);
}
