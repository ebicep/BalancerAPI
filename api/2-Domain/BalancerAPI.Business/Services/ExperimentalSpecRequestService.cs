using System.Text.Json;
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

    Task<ExperimentalSpecRequestUnrunServiceResult> UnrunFromBalanceIdAsync(
        Guid balanceId,
        CancellationToken cancellationToken);
}

public sealed class ExperimentalSpecRequestService(IDbContextFactory<BalancerDbContext> dbContextFactory)
    : IExperimentalSpecRequestService
{
    private const int InitialGameCooldown = 5;
    private const int UnrunGameCooldown = -10;
    private const int UnrunCreatedTimeYearsBeforeGame = 20;

    private static readonly JsonSerializerOptions BalanceJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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

    public async Task<ExperimentalSpecRequestUnrunServiceResult> UnrunFromBalanceIdAsync(
        Guid balanceId,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var log = await db.ExperimentalBalanceLogs
            .AsNoTracking()
            .Where(x => x.BalanceId == balanceId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (log is null)
        {
            return UnrunFail(404, "Balance log not found.");
        }

        List<ExperimentalBalanceTeam>? teams;
        try
        {
            teams = JsonSerializer.Deserialize<List<ExperimentalBalanceTeam>>(log.Balance, BalanceJsonOptions);
        }
        catch (JsonException)
        {
            return UnrunFail(400, "Stored balance JSON is invalid.");
        }

        if (teams is null || teams.Count == 0)
        {
            return UnrunFail(400, "Stored balance must contain at least one team.");
        }

        return await UnrunFromTeamsAsync(db, teams, log.CreatedAt, cancellationToken);
    }

    private static DateTime UnrunCreatedTimeFromGame(DateTime gameCreatedAt) =>
        gameCreatedAt.ToUniversalTime().AddYears(-UnrunCreatedTimeYearsBeforeGame);

    private async Task<ExperimentalSpecRequestUnrunServiceResult> UnrunFromTeamsAsync(
        BalancerDbContext db,
        IReadOnlyList<ExperimentalBalanceTeam> teams,
        DateTime gameCreatedAt,
        CancellationToken cancellationToken)
    {
        var unrunCreatedTime = UnrunCreatedTimeFromGame(gameCreatedAt);
        var requestedByUuid = new Dictionary<Guid, (string Spec, string Name)>();
        foreach (var team in teams)
        {
            foreach (var player in team.Specs)
            {
                if (player.Requested)
                {
                    requestedByUuid[player.Uuid] = (player.Spec, player.Name);
                }
            }
        }

        var canonicalByUuid = new Dictionary<Guid, (string Spec, string Name)>();
        foreach (var (uuid, requested) in requestedByUuid)
        {
            var canonical = ManualWeightAdjustmentService.TryNormalizeSpec(requested.Spec);
            if (canonical is null)
            {
                return UnrunFail(400, "Unknown or missing spec.");
            }

            canonicalByUuid[uuid] = (canonical, requested.Name);
        }

        foreach (var (uuid, entry) in canonicalByUuid)
        {
            var banRow = await db.ExperimentalSpecBans
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Uuid == uuid, cancellationToken);
            if (ExperimentalSpecBanFlags.IsBanned(banRow, entry.Spec))
            {
                return UnrunFail(400, $"Player is banned from {entry.Spec}.");
            }
        }

        var restored = new List<ExperimentalSpecRequestUnrunPlayerResponse>();
        foreach (var (uuid, entry) in canonicalByUuid)
        {
            var canonicalSpec = entry.Spec;
            var row = await db.ExperimentalSpecRequests
                .FirstOrDefaultAsync(x => x.Uuid == uuid, cancellationToken);

            if (row is null)
            {
                row = new ExperimentalSpecRequest
                {
                    Uuid = uuid,
                    Spec = canonicalSpec,
                    GameCooldown = UnrunGameCooldown,
                    CreatedTime = unrunCreatedTime
                };
                db.ExperimentalSpecRequests.Add(row);
            }
            else
            {
                row.Spec = canonicalSpec;
                row.GameCooldown = UnrunGameCooldown;
                row.CreatedTime = unrunCreatedTime;
            }

            restored.Add(new ExperimentalSpecRequestUnrunPlayerResponse(
                row.Uuid,
                entry.Name,
                row.Spec,
                row.GameCooldown,
                row.CreatedTime));
        }

        await db.SaveChangesAsync(cancellationToken);

        return UnrunOk(new ExperimentalSpecRequestUnrunResponse(restored));
    }

    private static ExperimentalSpecRequestServiceResult Ok(ExperimentalSpecRequestResponse data) =>
        new(true, 200, null, data);

    private static ExperimentalSpecRequestServiceResult Fail(int statusCode, string message) =>
        new(false, statusCode, message, null);

    private static ExperimentalSpecRequestUnrunServiceResult UnrunOk(ExperimentalSpecRequestUnrunResponse data) =>
        new(true, 200, null, data);

    private static ExperimentalSpecRequestUnrunServiceResult UnrunFail(int statusCode, string message) =>
        new(false, statusCode, message, null);
}
