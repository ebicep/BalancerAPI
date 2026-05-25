using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Business.Services;

public interface IExperimentalSpecRequestService
{
    Task<ExperimentalSpecRequestServiceResult> UpsertAsync(
        Guid uuid,
        string canonicalSpec,
        CancellationToken cancellationToken);
}

public sealed class ExperimentalSpecRequestService(IDbContextFactory<BalancerDbContext> dbContextFactory)
    : IExperimentalSpecRequestService
{
    private const int InitialGameCooldown = 5;

    public async Task<ExperimentalSpecRequestServiceResult> UpsertAsync(
        Guid uuid,
        string canonicalSpec,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var banRow = await db.ExperimentalSpecBans
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Uuid == uuid, cancellationToken);
        if (ExperimentalSpecBanFlags.IsBanned(banRow, canonicalSpec))
        {
            return Fail(400, $"Player is banned from {canonicalSpec}.");
        }

        var row = await db.ExperimentalSpecRequests
            .FirstOrDefaultAsync(x => x.Uuid == uuid, cancellationToken);

        if (row is null)
        {
            row = new ExperimentalSpecRequest
            {
                Uuid = uuid,
                Spec = canonicalSpec,
                GameCooldown = InitialGameCooldown,
                CreatedTime = DateTime.UtcNow
            };
            db.ExperimentalSpecRequests.Add(row);
        }
        else
        {
            row.Spec = canonicalSpec;
        }

        await db.SaveChangesAsync(cancellationToken);

        return Ok(new ExperimentalSpecRequestResponse(
            row.Uuid,
            row.Spec,
            row.GameCooldown,
            row.CreatedTime));
    }

    private static ExperimentalSpecRequestServiceResult Ok(ExperimentalSpecRequestResponse data) =>
        new(true, 200, null, data);

    private static ExperimentalSpecRequestServiceResult Fail(int statusCode, string message) =>
        new(false, statusCode, message, null);
}
