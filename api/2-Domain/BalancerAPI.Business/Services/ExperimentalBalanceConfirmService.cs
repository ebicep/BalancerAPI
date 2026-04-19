using System.Data;
using System.Text.Json;
using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Business.Services;

public sealed class ExperimentalBalanceConfirmService(IDbContextFactory<BalancerDbContext> dbContextFactory)
    : IExperimentalBalanceConfirmService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<ExperimentalBalanceConfirmServiceResult> ConfirmAsync(
        Guid balanceId,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var log = await db.ExperimentalBalanceLogs
            .Where(x => x.BalanceId == balanceId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (log is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return new ExperimentalBalanceConfirmServiceResult(false, 404, "Balance log not found.");
        }

        if (log.Posted)
        {
            await tx.RollbackAsync(cancellationToken);
            return new ExperimentalBalanceConfirmServiceResult(false, 409, "Balance already confirmed.");
        }

        List<ExperimentalBalanceTeam>? teams;
        try
        {
            teams = JsonSerializer.Deserialize<List<ExperimentalBalanceTeam>>(log.Balance, JsonOptions);
        }
        catch (JsonException)
        {
            await tx.RollbackAsync(cancellationToken);
            return new ExperimentalBalanceConfirmServiceResult(false, 400, "Stored balance JSON is invalid.");
        }

        if (teams is null || teams.Count == 0)
        {
            await tx.RollbackAsync(cancellationToken);
            return new ExperimentalBalanceConfirmServiceResult(false, 400, "Stored balance must contain at least one team.");
        }

        foreach (var team in teams)
        {
            var row = new ExperimentalSpecLog { BalanceId = balanceId };
            var specsUsed = new HashSet<string>(StringComparer.Ordinal);
            foreach (var player in team.Specs)
            {
                if (!specsUsed.Add(player.Spec))
                {
                    await tx.RollbackAsync(cancellationToken);
                    return new ExperimentalBalanceConfirmServiceResult(
                        false,
                        400,
                        $"Duplicate spec '{player.Spec}' within a team.");
                }

                if (player.Spec == ExperimentalSpecs.Empty)
                {
                    await tx.RollbackAsync(cancellationToken);
                    return new ExperimentalBalanceConfirmServiceResult(
                        false,
                        400,
                        "Empty spec is not allowed in balance log.");
                }

                if (!TryAssignSpec(row, player.Spec, player.Uuid))
                {
                    await tx.RollbackAsync(cancellationToken);
                    return new ExperimentalBalanceConfirmServiceResult(
                        false,
                        400,
                        $"Unknown spec '{player.Spec}'.");
                }
            }

            db.ExperimentalSpecLogs.Add(row);
        }

        log.Posted = true;
        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return new ExperimentalBalanceConfirmServiceResult(true, 200, null);
    }

    public async Task<ExperimentalBalanceConfirmServiceResult> UnconfirmAsync(
        Guid balanceId,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var log = await db.ExperimentalBalanceLogs
            .Where(x => x.BalanceId == balanceId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (log is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return new ExperimentalBalanceConfirmServiceResult(false, 404, "Balance log not found.");
        }

        if (!log.Posted)
        {
            await tx.RollbackAsync(cancellationToken);
            return new ExperimentalBalanceConfirmServiceResult(false, 409, "Balance already unconfirmed.");
        }

        if (log.Counted)
        {
            await tx.RollbackAsync(cancellationToken);
            return new ExperimentalBalanceConfirmServiceResult(false, 409, "Balance must be uninput before unconfirm.");
        }

        var specRows = await db.ExperimentalSpecLogs
            .Where(x => x.BalanceId == balanceId)
            .ToListAsync(cancellationToken);
        db.ExperimentalSpecLogs.RemoveRange(specRows);

        log.Posted = false;

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return new ExperimentalBalanceConfirmServiceResult(true, 200, null);
    }

    private static bool TryAssignSpec(ExperimentalSpecLog row, string spec, Guid uuid)
    {
        switch (spec)
        {
            case "Pyromancer": row.Pyromancer = uuid; return true;
            case "Cryomancer": row.Cryomancer = uuid; return true;
            case "Aquamancer": row.Aquamancer = uuid; return true;
            case "Berserker": row.Berserker = uuid; return true;
            case "Defender": row.Defender = uuid; return true;
            case "Revenant": row.Revenant = uuid; return true;
            case "Avenger": row.Avenger = uuid; return true;
            case "Crusader": row.Crusader = uuid; return true;
            case "Protector": row.Protector = uuid; return true;
            case "Thunderlord": row.Thunderlord = uuid; return true;
            case "Spiritguard": row.Spiritguard = uuid; return true;
            case "Earthwarden": row.Earthwarden = uuid; return true;
            case "Assassin": row.Assassin = uuid; return true;
            case "Vindicator": row.Vindicator = uuid; return true;
            case "Apothecary": row.Apothecary = uuid; return true;
            case "Conjurer": row.Conjurer = uuid; return true;
            case "Sentinel": row.Sentinel = uuid; return true;
            case "Luminary": row.Luminary = uuid; return true;
            default: return false;
        }
    }
}
