using System.Data;
using System.Text.Json;
using System.Text.Json.Nodes;
using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Business.Services;

public sealed class ExperimentalBalanceInputService(IDbContextFactory<BalancerDbContext> dbContextFactory)
    : IExperimentalBalanceInputService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed record InputApplicationContext(
        IReadOnlyList<ExperimentalBalanceInputPlayerLine> Winners,
        IReadOnlyList<ExperimentalBalanceInputPlayerLine> Losers,
        Dictionary<Guid, string> SpecByPlayer,
        Dictionary<Guid, ExperimentalSpecsWl> WlByUuid);

    public async Task<ExperimentalBalanceInputServiceResult> InputAsync(
        Guid balanceId,
        ExperimentalBalanceInputBody body,
        CancellationToken cancellationToken)
    {
        var requestedGameId = ExperimentalBalanceLogGameIds.TryNormalize(body.GameId);
        if (requestedGameId is null)
        {
            return Fail(400, "game_id must be a 24-character hexadecimal MongoDB ObjectId.");
        }

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var log = await db.ExperimentalBalanceLogs.FindAsync([balanceId], cancellationToken);
        if (log is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return Fail(404, "Balance log not found.");
        }

        if (!log.Posted)
        {
            await tx.RollbackAsync(cancellationToken);
            return Fail(409, "Balance must be confirmed before input.");
        }

        if (log.Counted)
        {
            await tx.RollbackAsync(cancellationToken);
            return Fail(409, "Balance result already counted.");
        }

        if (log.GameId is not null && log.GameId != requestedGameId)
        {
            await tx.RollbackAsync(cancellationToken);
            return Fail(400, "game_id does not match the stored game for this balance.");
        }

        if (log.Input is not null && !StoredInputMatchesRequest(log.Input, body))
        {
            await tx.RollbackAsync(cancellationToken);
            return Fail(400, "Request body must match the stored input JSON for this balance.");
        }

        var (ctxError, ctx) = await TryBuildInputApplicationContextAsync(db, balanceId, log.Balance, body, cancellationToken);
        if (ctxError is not null)
        {
            await tx.RollbackAsync(cancellationToken);
            return ctxError;
        }

        ApplyInputLines(ctx!.Winners, ctx.WlByUuid, ctx.SpecByPlayer, ApplyWin);
        ApplyInputLines(ctx.Losers, ctx.WlByUuid, ctx.SpecByPlayer, ApplyLoss);

        var canonicalInputJson = JsonSerializer.Serialize(body, JsonOptions);
        if (log.Input is null)
        {
            log.Input = canonicalInputJson;
        }

        log.GameId = requestedGameId;
        log.Counted = true;

        db.ExperimentalInputLogs.Add(new ExperimentalInputLog
        {
            BalanceId = balanceId,
            GameId = requestedGameId,
            Action = "input",
            OccurredAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return new ExperimentalBalanceInputServiceResult(true, 200, null);
    }

    public async Task<ExperimentalBalanceInputServiceResult> UninputAsync(Guid balanceId, CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var log = await db.ExperimentalBalanceLogs.FindAsync([balanceId], cancellationToken);
        if (log is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return Fail(404, "Balance log not found.");
        }

        if (!log.Posted)
        {
            await tx.RollbackAsync(cancellationToken);
            return Fail(409, "Balance must be confirmed before uninput.");
        }

        if (!log.Counted)
        {
            await tx.RollbackAsync(cancellationToken);
            return Fail(409, "Balance result not counted.");
        }

        if (string.IsNullOrEmpty(log.Input))
        {
            await tx.RollbackAsync(cancellationToken);
            return Fail(400, "No stored input to reverse.");
        }

        ExperimentalBalanceInputBody? body;
        try
        {
            body = JsonSerializer.Deserialize<ExperimentalBalanceInputBody>(log.Input, JsonOptions);
        }
        catch (JsonException)
        {
            await tx.RollbackAsync(cancellationToken);
            return Fail(400, "Stored input JSON is invalid.");
        }

        if (body is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return Fail(400, "Stored input JSON is invalid.");
        }

        var storedGameId = ExperimentalBalanceLogGameIds.TryNormalize(body.GameId);
        if (storedGameId is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return Fail(400, "Stored input JSON has an invalid game_id.");
        }

        if (log.GameId is not null && log.GameId != storedGameId)
        {
            await tx.RollbackAsync(cancellationToken);
            return Fail(400, "Stored input game_id does not match the balance log.");
        }

        var (ctxError, ctx) = await TryBuildInputApplicationContextAsync(db, balanceId, log.Balance, body, cancellationToken);
        if (ctxError is not null)
        {
            await tx.RollbackAsync(cancellationToken);
            return ctxError;
        }

        if (!TryApplyReverseLines(ctx!.Winners, ctx.WlByUuid, ctx.SpecByPlayer, TryReverseWin)
            || !TryApplyReverseLines(ctx.Losers, ctx.WlByUuid, ctx.SpecByPlayer, TryReverseLoss))
        {
            await tx.RollbackAsync(cancellationToken);
            return Fail(409, "Cannot uninput; win/loss stats are inconsistent with the stored input.");
        }

        log.Counted = false;

        var auditGameId = log.GameId ?? storedGameId;
        db.ExperimentalInputLogs.Add(new ExperimentalInputLog
        {
            BalanceId = balanceId,
            GameId = auditGameId,
            Action = "uninput",
            OccurredAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return new ExperimentalBalanceInputServiceResult(true, 200, null);
    }

    public async Task<ExperimentalBalanceInputServiceResult> ClearInputAsync(Guid balanceId, CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var log = await db.ExperimentalBalanceLogs.FindAsync([balanceId], cancellationToken);
        if (log is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return Fail(404, "Balance log not found.");
        }

        if (log.Input is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return Fail(409, "Input is already null.");
        }

        ExperimentalBalanceInputBody? inputBody;
        try
        {
            inputBody = JsonSerializer.Deserialize<ExperimentalBalanceInputBody>(log.Input, JsonOptions);
        }
        catch (JsonException)
        {
            await tx.RollbackAsync(cancellationToken);
            return Fail(400, "Stored input JSON is invalid.");
        }

        if (inputBody is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return Fail(400, "Stored input JSON is invalid.");
        }

        var storedGameId = ExperimentalBalanceLogGameIds.TryNormalize(inputBody.GameId);
        if (storedGameId is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return Fail(400, "Stored input JSON has an invalid game_id.");
        }

        if (log.GameId is not null && log.GameId != storedGameId)
        {
            await tx.RollbackAsync(cancellationToken);
            return Fail(400, "Stored input game_id does not match the balance log.");
        }

        var auditGameId = log.GameId ?? storedGameId;

        log.Input = null;

        db.ExperimentalInputLogs.Add(new ExperimentalInputLog
        {
            BalanceId = balanceId,
            GameId = auditGameId,
            Action = "clear",
            OccurredAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return new ExperimentalBalanceInputServiceResult(true, 200, null);
    }

    private async Task<(ExperimentalBalanceInputServiceResult? Error, InputApplicationContext? Ctx)> TryBuildInputApplicationContextAsync(
        BalancerDbContext db,
        Guid balanceId,
        string balanceJson,
        ExperimentalBalanceInputBody body,
        CancellationToken cancellationToken)
    {
        var winners = body.Winners ?? [];
        var losers = body.Losers ?? [];

        List<ExperimentalBalanceTeam>? teams;
        try
        {
            teams = JsonSerializer.Deserialize<List<ExperimentalBalanceTeam>>(balanceJson, JsonOptions);
        }
        catch (JsonException)
        {
            return (Fail(400, "Stored balance JSON is invalid."), null);
        }

        if (teams is null || teams.Count < 2)
        {
            return (Fail(400, "Stored balance must contain at least two teams."), null);
        }

        var teamSets = new List<HashSet<Guid>>(teams.Count);
        teamSets.AddRange(teams.Select(team => team.Specs.Select(s => s.Uuid).ToHashSet()));

        var allBalance = new HashSet<Guid>();
        foreach (var ts in teamSets)
        {
            allBalance.UnionWith(ts);
        }

        var rosterError = TryValidateWinnerLoserPayload(winners, losers, allBalance, out var winnerSet, out var loserSet);
        if (rosterError is not null)
        {
            return (rosterError, null);
        }

        var submitted = new HashSet<Guid>(winnerSet);
        submitted.UnionWith(loserSet);
        if (!submitted.SetEquals(allBalance))
        {
            return (Fail(400, "winners and losers must include every player in the balance exactly once."), null);
        }

        var teamsValid = false;
        for (var i = 0; i < teamSets.Count; i++)
        {
            if (teamSets.Where((_, j) => i != j).Any(t => winnerSet.IsSubsetOf(teamSets[i]) && loserSet.IsSubsetOf(t)))
            {
                teamsValid = true;
            }

            if (teamsValid)
            {
                break;
            }
        }

        if (!teamsValid)
        {
            return (Fail(400, "All winners must be on one team and all losers on the other."), null);
        }

        var specRows = await db.ExperimentalSpecLogs
            .Where(x => x.BalanceId == balanceId)
            .ToListAsync(cancellationToken);

        if (specRows.Count == 0)
        {
            return (Fail(400, "No spec log rows for this balance."), null);
        }

        var specByPlayer = new Dictionary<Guid, string>();
        foreach (var uuid in submitted)
        {
            var spec = ExperimentalSpecLogLookup.FindSpecForUuid(specRows, uuid);
            if (spec is null)
            {
                return (Fail(400, "Missing spec assignment for one or more players."), null);
            }

            specByPlayer[uuid] = spec;
        }

        var uuidsList = submitted.ToList();
        var wlRows = await db.ExperimentalSpecsWl
            .Where(x => uuidsList.Contains(x.Uuid))
            .ToListAsync(cancellationToken);

        if (wlRows.Count != uuidsList.Count)
        {
            return (Fail(404, "One or more players are missing experimental_specs_wl rows."), null);
        }

        var wlByUuid = wlRows.ToDictionary(x => x.Uuid);
        return (null, new InputApplicationContext(winners, losers, specByPlayer, wlByUuid));
    }

    private static bool TryApplyReverseLines(
        IReadOnlyList<ExperimentalBalanceInputPlayerLine> lines,
        IReadOnlyDictionary<Guid, ExperimentalSpecsWl> wlByUuid,
        IReadOnlyDictionary<Guid, string> specByPlayer,
        Func<ExperimentalSpecsWl, string, int, int, bool> tryReverse)
    {
        foreach (var line in lines)
        {
            if (!tryReverse(wlByUuid[line.Uuid], specByPlayer[line.Uuid], line.Kills, line.Deaths))
            {
                return false;
            }
        }

        return true;
    }

    private static ExperimentalBalanceInputServiceResult Fail(int status, string message) =>
        new(false, status, message);

    private static bool StoredInputMatchesRequest(string storedJson, ExperimentalBalanceInputBody body)
    {
        var incoming = JsonNode.Parse(JsonSerializer.Serialize(body, JsonOptions))!;
        var stored = JsonNode.Parse(storedJson)!;
        return JsonNode.DeepEquals(stored, incoming);
    }

    /// <summary>Null when valid; on failure <paramref name="winnerSet"/> and <paramref name="loserSet"/> are empty.</summary>
    private static ExperimentalBalanceInputServiceResult? TryValidateWinnerLoserPayload(
        IReadOnlyList<ExperimentalBalanceInputPlayerLine> winners,
        IReadOnlyList<ExperimentalBalanceInputPlayerLine> losers,
        HashSet<Guid> allBalance,
        out HashSet<Guid> winnerSet,
        out HashSet<Guid> loserSet)
    {
        winnerSet = [];
        loserSet = [];

        if (winners.Count == 0 && losers.Count == 0)
        {
            return Fail(400, "winners and losers must not both be empty.");
        }

        if (winners.Select(w => w.Uuid).Distinct().Count() != winners.Count)
        {
            return Fail(400, "Duplicate UUID in winners.");
        }

        if (losers.Select(l => l.Uuid).Distinct().Count() != losers.Count)
        {
            return Fail(400, "Duplicate UUID in losers.");
        }

        winnerSet = winners.Select(w => w.Uuid).ToHashSet();
        loserSet = losers.Select(l => l.Uuid).ToHashSet();
        if (winnerSet.Overlaps(loserSet))
        {
            return Fail(400, "A player cannot appear in both winners and losers.");
        }

        foreach (var (line, roleLabel) in winners.Select(w => (w, "Winner")).Concat(losers.Select(l => (l, "Loser"))))
        {
            var lineError = ValidateKillsDeathsAndBalanceMembership(line, roleLabel, allBalance);
            if (lineError is not null)
            {
                return lineError;
            }
        }

        return null;
    }

    private static ExperimentalBalanceInputServiceResult? ValidateKillsDeathsAndBalanceMembership(
        ExperimentalBalanceInputPlayerLine line,
        string roleLabel,
        HashSet<Guid> allBalance)
    {
        if (line.Kills < 0 || line.Deaths < 0)
        {
            return Fail(400, "kills and deaths must be non-negative.");
        }

        if (!allBalance.Contains(line.Uuid))
        {
            return Fail(400, $"{roleLabel} UUID is not part of this balance.");
        }

        return null;
    }

    private static void ApplyInputLines(
        IReadOnlyList<ExperimentalBalanceInputPlayerLine> lines,
        IReadOnlyDictionary<Guid, ExperimentalSpecsWl> wlByUuid,
        IReadOnlyDictionary<Guid, string> specByPlayer,
        Action<ExperimentalSpecsWl, string, int, int> applySpecStats)
    {
        foreach (var line in lines)
        {
            applySpecStats(wlByUuid[line.Uuid], specByPlayer[line.Uuid], line.Kills, line.Deaths);
        }
    }

    private static void ApplyWin(ExperimentalSpecsWl row, string spec, int kills, int deaths)
    {
        switch (spec)
        {
            case "Pyromancer": row.PyromancerWins++; row.PyromancerKills += kills; row.PyromancerDeaths += deaths; break;
            case "Cryomancer": row.CryomancerWins++; row.CryomancerKills += kills; row.CryomancerDeaths += deaths; break;
            case "Aquamancer": row.AquamancerWins++; row.AquamancerKills += kills; row.AquamancerDeaths += deaths; break;
            case "Berserker": row.BerserkerWins++; row.BerserkerKills += kills; row.BerserkerDeaths += deaths; break;
            case "Defender": row.DefenderWins++; row.DefenderKills += kills; row.DefenderDeaths += deaths; break;
            case "Revenant": row.RevenantWins++; row.RevenantKills += kills; row.RevenantDeaths += deaths; break;
            case "Avenger": row.AvengerWins++; row.AvengerKills += kills; row.AvengerDeaths += deaths; break;
            case "Crusader": row.CrusaderWins++; row.CrusaderKills += kills; row.CrusaderDeaths += deaths; break;
            case "Protector": row.ProtectorWins++; row.ProtectorKills += kills; row.ProtectorDeaths += deaths; break;
            case "Thunderlord": row.ThunderlordWins++; row.ThunderlordKills += kills; row.ThunderlordDeaths += deaths; break;
            case "Spiritguard": row.SpiritguardWins++; row.SpiritguardKills += kills; row.SpiritguardDeaths += deaths; break;
            case "Earthwarden": row.EarthwardenWins++; row.EarthwardenKills += kills; row.EarthwardenDeaths += deaths; break;
            case "Assassin": row.AssassinWins++; row.AssassinKills += kills; row.AssassinDeaths += deaths; break;
            case "Vindicator": row.VindicatorWins++; row.VindicatorKills += kills; row.VindicatorDeaths += deaths; break;
            case "Apothecary": row.ApothecaryWins++; row.ApothecaryKills += kills; row.ApothecaryDeaths += deaths; break;
            case "Conjurer": row.ConjurerWins++; row.ConjurerKills += kills; row.ConjurerDeaths += deaths; break;
            case "Sentinel": row.SentinelWins++; row.SentinelKills += kills; row.SentinelDeaths += deaths; break;
            case "Luminary": row.LuminaryWins++; row.LuminaryKills += kills; row.LuminaryDeaths += deaths; break;
            default:
                throw new InvalidOperationException($"Unknown spec '{spec}'.");
        }
    }

    private static void ApplyLoss(ExperimentalSpecsWl row, string spec, int kills, int deaths)
    {
        switch (spec)
        {
            case "Pyromancer": row.PyromancerLosses++; row.PyromancerKills += kills; row.PyromancerDeaths += deaths; break;
            case "Cryomancer": row.CryomancerLosses++; row.CryomancerKills += kills; row.CryomancerDeaths += deaths; break;
            case "Aquamancer": row.AquamancerLosses++; row.AquamancerKills += kills; row.AquamancerDeaths += deaths; break;
            case "Berserker": row.BerserkerLosses++; row.BerserkerKills += kills; row.BerserkerDeaths += deaths; break;
            case "Defender": row.DefenderLosses++; row.DefenderKills += kills; row.DefenderDeaths += deaths; break;
            case "Revenant": row.RevenantLosses++; row.RevenantKills += kills; row.RevenantDeaths += deaths; break;
            case "Avenger": row.AvengerLosses++; row.AvengerKills += kills; row.AvengerDeaths += deaths; break;
            case "Crusader": row.CrusaderLosses++; row.CrusaderKills += kills; row.CrusaderDeaths += deaths; break;
            case "Protector": row.ProtectorLosses++; row.ProtectorKills += kills; row.ProtectorDeaths += deaths; break;
            case "Thunderlord": row.ThunderlordLosses++; row.ThunderlordKills += kills; row.ThunderlordDeaths += deaths; break;
            case "Spiritguard": row.SpiritguardLosses++; row.SpiritguardKills += kills; row.SpiritguardDeaths += deaths; break;
            case "Earthwarden": row.EarthwardenLosses++; row.EarthwardenKills += kills; row.EarthwardenDeaths += deaths; break;
            case "Assassin": row.AssassinLosses++; row.AssassinKills += kills; row.AssassinDeaths += deaths; break;
            case "Vindicator": row.VindicatorLosses++; row.VindicatorKills += kills; row.VindicatorDeaths += deaths; break;
            case "Apothecary": row.ApothecaryLosses++; row.ApothecaryKills += kills; row.ApothecaryDeaths += deaths; break;
            case "Conjurer": row.ConjurerLosses++; row.ConjurerKills += kills; row.ConjurerDeaths += deaths; break;
            case "Sentinel": row.SentinelLosses++; row.SentinelKills += kills; row.SentinelDeaths += deaths; break;
            case "Luminary": row.LuminaryLosses++; row.LuminaryKills += kills; row.LuminaryDeaths += deaths; break;
            default:
                throw new InvalidOperationException($"Unknown spec '{spec}'.");
        }
    }

    private static bool TryReverseWin(ExperimentalSpecsWl row, string spec, int kills, int deaths)
    {
        switch (spec)
        {
            case "Pyromancer":
                if (row.PyromancerWins < 1 || row.PyromancerKills < kills || row.PyromancerDeaths < deaths) return false;
                row.PyromancerWins--; row.PyromancerKills -= kills; row.PyromancerDeaths -= deaths; return true;
            case "Cryomancer":
                if (row.CryomancerWins < 1 || row.CryomancerKills < kills || row.CryomancerDeaths < deaths) return false;
                row.CryomancerWins--; row.CryomancerKills -= kills; row.CryomancerDeaths -= deaths; return true;
            case "Aquamancer":
                if (row.AquamancerWins < 1 || row.AquamancerKills < kills || row.AquamancerDeaths < deaths) return false;
                row.AquamancerWins--; row.AquamancerKills -= kills; row.AquamancerDeaths -= deaths; return true;
            case "Berserker":
                if (row.BerserkerWins < 1 || row.BerserkerKills < kills || row.BerserkerDeaths < deaths) return false;
                row.BerserkerWins--; row.BerserkerKills -= kills; row.BerserkerDeaths -= deaths; return true;
            case "Defender":
                if (row.DefenderWins < 1 || row.DefenderKills < kills || row.DefenderDeaths < deaths) return false;
                row.DefenderWins--; row.DefenderKills -= kills; row.DefenderDeaths -= deaths; return true;
            case "Revenant":
                if (row.RevenantWins < 1 || row.RevenantKills < kills || row.RevenantDeaths < deaths) return false;
                row.RevenantWins--; row.RevenantKills -= kills; row.RevenantDeaths -= deaths; return true;
            case "Avenger":
                if (row.AvengerWins < 1 || row.AvengerKills < kills || row.AvengerDeaths < deaths) return false;
                row.AvengerWins--; row.AvengerKills -= kills; row.AvengerDeaths -= deaths; return true;
            case "Crusader":
                if (row.CrusaderWins < 1 || row.CrusaderKills < kills || row.CrusaderDeaths < deaths) return false;
                row.CrusaderWins--; row.CrusaderKills -= kills; row.CrusaderDeaths -= deaths; return true;
            case "Protector":
                if (row.ProtectorWins < 1 || row.ProtectorKills < kills || row.ProtectorDeaths < deaths) return false;
                row.ProtectorWins--; row.ProtectorKills -= kills; row.ProtectorDeaths -= deaths; return true;
            case "Thunderlord":
                if (row.ThunderlordWins < 1 || row.ThunderlordKills < kills || row.ThunderlordDeaths < deaths) return false;
                row.ThunderlordWins--; row.ThunderlordKills -= kills; row.ThunderlordDeaths -= deaths; return true;
            case "Spiritguard":
                if (row.SpiritguardWins < 1 || row.SpiritguardKills < kills || row.SpiritguardDeaths < deaths) return false;
                row.SpiritguardWins--; row.SpiritguardKills -= kills; row.SpiritguardDeaths -= deaths; return true;
            case "Earthwarden":
                if (row.EarthwardenWins < 1 || row.EarthwardenKills < kills || row.EarthwardenDeaths < deaths) return false;
                row.EarthwardenWins--; row.EarthwardenKills -= kills; row.EarthwardenDeaths -= deaths; return true;
            case "Assassin":
                if (row.AssassinWins < 1 || row.AssassinKills < kills || row.AssassinDeaths < deaths) return false;
                row.AssassinWins--; row.AssassinKills -= kills; row.AssassinDeaths -= deaths; return true;
            case "Vindicator":
                if (row.VindicatorWins < 1 || row.VindicatorKills < kills || row.VindicatorDeaths < deaths) return false;
                row.VindicatorWins--; row.VindicatorKills -= kills; row.VindicatorDeaths -= deaths; return true;
            case "Apothecary":
                if (row.ApothecaryWins < 1 || row.ApothecaryKills < kills || row.ApothecaryDeaths < deaths) return false;
                row.ApothecaryWins--; row.ApothecaryKills -= kills; row.ApothecaryDeaths -= deaths; return true;
            case "Conjurer":
                if (row.ConjurerWins < 1 || row.ConjurerKills < kills || row.ConjurerDeaths < deaths) return false;
                row.ConjurerWins--; row.ConjurerKills -= kills; row.ConjurerDeaths -= deaths; return true;
            case "Sentinel":
                if (row.SentinelWins < 1 || row.SentinelKills < kills || row.SentinelDeaths < deaths) return false;
                row.SentinelWins--; row.SentinelKills -= kills; row.SentinelDeaths -= deaths; return true;
            case "Luminary":
                if (row.LuminaryWins < 1 || row.LuminaryKills < kills || row.LuminaryDeaths < deaths) return false;
                row.LuminaryWins--; row.LuminaryKills -= kills; row.LuminaryDeaths -= deaths; return true;
            default:
                throw new InvalidOperationException($"Unknown spec '{spec}'.");
        }
    }

    private static bool TryReverseLoss(ExperimentalSpecsWl row, string spec, int kills, int deaths)
    {
        switch (spec)
        {
            case "Pyromancer":
                if (row.PyromancerLosses < 1 || row.PyromancerKills < kills || row.PyromancerDeaths < deaths) return false;
                row.PyromancerLosses--; row.PyromancerKills -= kills; row.PyromancerDeaths -= deaths; return true;
            case "Cryomancer":
                if (row.CryomancerLosses < 1 || row.CryomancerKills < kills || row.CryomancerDeaths < deaths) return false;
                row.CryomancerLosses--; row.CryomancerKills -= kills; row.CryomancerDeaths -= deaths; return true;
            case "Aquamancer":
                if (row.AquamancerLosses < 1 || row.AquamancerKills < kills || row.AquamancerDeaths < deaths) return false;
                row.AquamancerLosses--; row.AquamancerKills -= kills; row.AquamancerDeaths -= deaths; return true;
            case "Berserker":
                if (row.BerserkerLosses < 1 || row.BerserkerKills < kills || row.BerserkerDeaths < deaths) return false;
                row.BerserkerLosses--; row.BerserkerKills -= kills; row.BerserkerDeaths -= deaths; return true;
            case "Defender":
                if (row.DefenderLosses < 1 || row.DefenderKills < kills || row.DefenderDeaths < deaths) return false;
                row.DefenderLosses--; row.DefenderKills -= kills; row.DefenderDeaths -= deaths; return true;
            case "Revenant":
                if (row.RevenantLosses < 1 || row.RevenantKills < kills || row.RevenantDeaths < deaths) return false;
                row.RevenantLosses--; row.RevenantKills -= kills; row.RevenantDeaths -= deaths; return true;
            case "Avenger":
                if (row.AvengerLosses < 1 || row.AvengerKills < kills || row.AvengerDeaths < deaths) return false;
                row.AvengerLosses--; row.AvengerKills -= kills; row.AvengerDeaths -= deaths; return true;
            case "Crusader":
                if (row.CrusaderLosses < 1 || row.CrusaderKills < kills || row.CrusaderDeaths < deaths) return false;
                row.CrusaderLosses--; row.CrusaderKills -= kills; row.CrusaderDeaths -= deaths; return true;
            case "Protector":
                if (row.ProtectorLosses < 1 || row.ProtectorKills < kills || row.ProtectorDeaths < deaths) return false;
                row.ProtectorLosses--; row.ProtectorKills -= kills; row.ProtectorDeaths -= deaths; return true;
            case "Thunderlord":
                if (row.ThunderlordLosses < 1 || row.ThunderlordKills < kills || row.ThunderlordDeaths < deaths) return false;
                row.ThunderlordLosses--; row.ThunderlordKills -= kills; row.ThunderlordDeaths -= deaths; return true;
            case "Spiritguard":
                if (row.SpiritguardLosses < 1 || row.SpiritguardKills < kills || row.SpiritguardDeaths < deaths) return false;
                row.SpiritguardLosses--; row.SpiritguardKills -= kills; row.SpiritguardDeaths -= deaths; return true;
            case "Earthwarden":
                if (row.EarthwardenLosses < 1 || row.EarthwardenKills < kills || row.EarthwardenDeaths < deaths) return false;
                row.EarthwardenLosses--; row.EarthwardenKills -= kills; row.EarthwardenDeaths -= deaths; return true;
            case "Assassin":
                if (row.AssassinLosses < 1 || row.AssassinKills < kills || row.AssassinDeaths < deaths) return false;
                row.AssassinLosses--; row.AssassinKills -= kills; row.AssassinDeaths -= deaths; return true;
            case "Vindicator":
                if (row.VindicatorLosses < 1 || row.VindicatorKills < kills || row.VindicatorDeaths < deaths) return false;
                row.VindicatorLosses--; row.VindicatorKills -= kills; row.VindicatorDeaths -= deaths; return true;
            case "Apothecary":
                if (row.ApothecaryLosses < 1 || row.ApothecaryKills < kills || row.ApothecaryDeaths < deaths) return false;
                row.ApothecaryLosses--; row.ApothecaryKills -= kills; row.ApothecaryDeaths -= deaths; return true;
            case "Conjurer":
                if (row.ConjurerLosses < 1 || row.ConjurerKills < kills || row.ConjurerDeaths < deaths) return false;
                row.ConjurerLosses--; row.ConjurerKills -= kills; row.ConjurerDeaths -= deaths; return true;
            case "Sentinel":
                if (row.SentinelLosses < 1 || row.SentinelKills < kills || row.SentinelDeaths < deaths) return false;
                row.SentinelLosses--; row.SentinelKills -= kills; row.SentinelDeaths -= deaths; return true;
            case "Luminary":
                if (row.LuminaryLosses < 1 || row.LuminaryKills < kills || row.LuminaryDeaths < deaths) return false;
                row.LuminaryLosses--; row.LuminaryKills -= kills; row.LuminaryDeaths -= deaths; return true;
            default:
                throw new InvalidOperationException($"Unknown spec '{spec}'.");
        }
    }
}

internal static class ExperimentalSpecLogLookup
{
    public static string? FindSpecForUuid(IEnumerable<ExperimentalSpecLog> rows, Guid uuid)
    {
        foreach (var row in rows)
        {
            if (row.Pyromancer == uuid) return "Pyromancer";
            if (row.Cryomancer == uuid) return "Cryomancer";
            if (row.Aquamancer == uuid) return "Aquamancer";
            if (row.Berserker == uuid) return "Berserker";
            if (row.Defender == uuid) return "Defender";
            if (row.Revenant == uuid) return "Revenant";
            if (row.Avenger == uuid) return "Avenger";
            if (row.Crusader == uuid) return "Crusader";
            if (row.Protector == uuid) return "Protector";
            if (row.Thunderlord == uuid) return "Thunderlord";
            if (row.Spiritguard == uuid) return "Spiritguard";
            if (row.Earthwarden == uuid) return "Earthwarden";
            if (row.Assassin == uuid) return "Assassin";
            if (row.Vindicator == uuid) return "Vindicator";
            if (row.Apothecary == uuid) return "Apothecary";
            if (row.Conjurer == uuid) return "Conjurer";
            if (row.Sentinel == uuid) return "Sentinel";
            if (row.Luminary == uuid) return "Luminary";
        }

        return null;
    }
}
