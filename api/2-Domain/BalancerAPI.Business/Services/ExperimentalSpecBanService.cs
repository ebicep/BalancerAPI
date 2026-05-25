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

    public async Task<ExperimentalSpecBanServiceResult> SetBanAsync(
        Guid uuid,
        string canonicalSpec,
        bool banned,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var row = await db.ExperimentalSpecBans
            .FirstOrDefaultAsync(x => x.Uuid == uuid, cancellationToken);

        if (!banned)
        {
            if (row is null || !ExperimentalSpecBanFlags.GetBanFlag(row, canonicalSpec))
            {
                return Fail(400, $"Player is not banned from {canonicalSpec}.");
            }
        }
        else if (row is not null && ExperimentalSpecBanFlags.GetBanFlag(row, canonicalSpec))
        {
            return Fail(400, $"Player is already banned from {canonicalSpec}.");
        }

        if (row is null)
        {
            row = new ExperimentalSpecBan { Uuid = uuid };
            db.ExperimentalSpecBans.Add(row);
        }

        SetBanFlag(row, canonicalSpec, banned);

        if (banned)
        {
            var matchingRequest = await db.ExperimentalSpecRequests
                .FirstOrDefaultAsync(
                    x => x.Uuid == uuid && x.Spec == canonicalSpec,
                    cancellationToken);
            if (matchingRequest is not null)
            {
                db.ExperimentalSpecRequests.Remove(matchingRequest);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        return Ok(BansFromRow(row));
    }

    private static ExperimentalSpecBanServiceResult Ok(IReadOnlyList<string> bans) =>
        new(true, 200, null, new ExperimentalSpecBansResponse(bans));

    private static ExperimentalSpecBanServiceResult Fail(int statusCode, string message) =>
        new(false, statusCode, message, null);

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
