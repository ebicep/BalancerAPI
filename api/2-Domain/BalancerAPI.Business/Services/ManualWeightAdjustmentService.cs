using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Business.Services;

public sealed class ManualWeightAdjustmentService(BalancerDbContext dbContext) : IManualWeightAdjustmentService
{
    public async Task<ManualWeightAdjustServiceResult<ManualBaseAdjustResponse>> PatchBaseAsync(
        string playerKey,
        ManualAdjustBaseRequest body,
        CancellationToken cancellationToken)
    {
        var resolved = await ResolvePlayerKeyAsync(playerKey, cancellationToken);
        if (!resolved.Success)
        {
            return ManualWeightAdjustServiceResult<ManualBaseAdjustResponse>.Fail(
                resolved.StatusCode,
                resolved.Message!);
        }

        var uuid = resolved.Uuid!.Value;
        var displayName = resolved.DisplayName ?? string.Empty;
        var row = await (
            from bw in dbContext.BaseWeights
            where bw.Uuid == uuid
            select bw
        ).AsTracking().FirstOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            return ManualWeightAdjustServiceResult<ManualBaseAdjustResponse>.Fail(
                404,
                "Base weight row not found for player.");
        }

        var baseWeight = row;
        var previousWeight = baseWeight.Weight;
        baseWeight.Weight += body.Amount;
        var recordedAt = DateTime.UtcNow;
        var response = new ManualBaseAdjustResponse(uuid, displayName, previousWeight, baseWeight.Weight);
        dbContext.AdjustmentManualDailyLogs.Add(new AdjustmentManualDailyLog
        {
            Id = Guid.NewGuid(),
            Uuid = uuid,
            PreviousWeight = response.PreviousWeight,
            NewWeight = response.NewWeight,
            Date = recordedAt
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return ManualWeightAdjustServiceResult<ManualBaseAdjustResponse>.Ok(response);
    }

    public async Task<ManualWeightAdjustServiceResult<ManualSpecAdjustResponse>> PatchSpecAsync(
        string playerKey,
        ManualAdjustSpecRequest body,
        CancellationToken cancellationToken)
    {
        var canonicalSpec = TryNormalizeSpec(body.Spec);
        if (canonicalSpec is null)
        {
            return ManualWeightAdjustServiceResult<ManualSpecAdjustResponse>.Fail(
                400,
                "Unknown or missing spec.");
        }

        var resolved = await ResolvePlayerKeyAsync(playerKey, cancellationToken);
        if (!resolved.Success)
        {
            return ManualWeightAdjustServiceResult<ManualSpecAdjustResponse>.Fail(
                resolved.StatusCode,
                resolved.Message!);
        }

        var uuid = resolved.Uuid!.Value;
        var displayName = resolved.DisplayName ?? string.Empty;
        var row = await (
            from sw in dbContext.ExperimentalSpecWeights
            where sw.Uuid == uuid
            join bw in dbContext.BaseWeights.AsNoTracking() on sw.Uuid equals bw.Uuid into baseJoin
            from bw in baseJoin.DefaultIfEmpty()
            select new
            {
                SpecRow = sw,
                BaseWeight = bw
            }
        ).AsTracking().FirstOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            return ManualWeightAdjustServiceResult<ManualSpecAdjustResponse>.Fail(
                404,
                "Experimental spec weight row not found for player.");
        }

        if (row.BaseWeight is null)
        {
            return ManualWeightAdjustServiceResult<ManualSpecAdjustResponse>.Fail(
                404,
                "Base weight row not found for player.");
        }

        var specRow = row.SpecRow;
        var baseWeight = row.BaseWeight;
        var previousOffset = GetOffset(specRow, canonicalSpec);
        var previousSpecWeight = baseWeight.Weight - previousOffset;
        AddToOffset(specRow, canonicalSpec, body.Amount);
        var newOffset = GetOffset(specRow, canonicalSpec);
        var newSpecWeight = baseWeight.Weight - newOffset;
        var recordedAt = DateTime.UtcNow;
        var response = new ManualSpecAdjustResponse(
            uuid,
            displayName,
            canonicalSpec,
            previousOffset,
            newOffset,
            baseWeight.Weight,
            previousSpecWeight,
            newSpecWeight);
        dbContext.AdjustmentManualWeeklyLogs.Add(new AdjustmentManualWeeklyLog
        {
            Id = Guid.NewGuid(),
            Uuid = uuid,
            Spec = canonicalSpec,
            PreviousOffset = response.PreviousOffset,
            NewOffset = response.NewOffset,
            BaseWeight = response.BaseWeight,
            PreviousSpecWeight = response.PreviousSpecWeight,
            NewSpecWeight = response.NewSpecWeight,
            Date = recordedAt
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return ManualWeightAdjustServiceResult<ManualSpecAdjustResponse>.Ok(response);
    }

    private async Task<(bool Success, int StatusCode, string? Message, Guid? Uuid, string? DisplayName)> ResolvePlayerKeyAsync(
        string playerKey,
        CancellationToken cancellationToken)
    {
        var trimmed = playerKey.Trim();
        if (trimmed.Length == 0)
        {
            return (false, 400, "Player identifier is required.", null, null);
        }

        if (Guid.TryParse(trimmed, out var uuid))
        {
            var namesByUuid = await dbContext.Names.AsNoTracking()
                .Where(x => x.Uuid == uuid)
                .Select(x => x.Name)
                .Distinct()
                .ToListAsync(cancellationToken);
            if (namesByUuid.Count > 1)
            {
                return (false, 409, $"Player UUID has multiple names in names table: {uuid}.", null, null);
            }

            var displayName = namesByUuid.Count == 1 ? namesByUuid[0] : string.Empty;
            return (true, 200, null, uuid, displayName);
        }

        var normalizedName = trimmed.ToLowerInvariant();
        var rows = await dbContext.Names.AsNoTracking()
            .Where(x => x.Name.ToLower() == normalizedName)
            .Select(x => new { x.Uuid, x.Name })
            .Distinct()
            .ToListAsync(cancellationToken);

        return rows.Count switch
        {
            > 1 => (false, 409, $"Player name is ambiguous in names table: {trimmed}.", null, null),
            0 => (false, 404, $"No matching UUID found in names table for: {trimmed}.", null, null),
            _ => (true, 200, null, rows[0].Uuid, rows[0].Name)
        };
    }

    internal static string? TryNormalizeSpec(string? spec)
    {
        if (string.IsNullOrWhiteSpace(spec))
        {
            return null;
        }

        var trimmed = spec.Trim();
        return ExperimentalSpecs.AllOrdered.FirstOrDefault(s => string.Equals(s, trimmed, StringComparison.OrdinalIgnoreCase));
    }

    private static int GetOffset(ExperimentalSpecWeight sw, string spec) =>
        spec switch
        {
            "Pyromancer" => sw.PyromancerOffset,
            "Cryomancer" => sw.CryomancerOffset,
            "Aquamancer" => sw.AquamancerOffset,
            "Berserker" => sw.BerserkerOffset,
            "Defender" => sw.DefenderOffset,
            "Revenant" => sw.RevenantOffset,
            "Avenger" => sw.AvengerOffset,
            "Crusader" => sw.CrusaderOffset,
            "Protector" => sw.ProtectorOffset,
            "Thunderlord" => sw.ThunderlordOffset,
            "Spiritguard" => sw.SpiritguardOffset,
            "Earthwarden" => sw.EarthwardenOffset,
            "Assassin" => sw.AssassinOffset,
            "Vindicator" => sw.VindicatorOffset,
            "Apothecary" => sw.ApothecaryOffset,
            "Conjurer" => sw.ConjurerOffset,
            "Sentinel" => sw.SentinelOffset,
            "Luminary" => sw.LuminaryOffset,
            _ => 0
        };

    private static void AddToOffset(ExperimentalSpecWeight sw, string spec, int amount)
    {
        switch (spec)
        {
            case "Pyromancer":
                sw.PyromancerOffset += amount;
                break;
            case "Cryomancer":
                sw.CryomancerOffset += amount;
                break;
            case "Aquamancer":
                sw.AquamancerOffset += amount;
                break;
            case "Berserker":
                sw.BerserkerOffset += amount;
                break;
            case "Defender":
                sw.DefenderOffset += amount;
                break;
            case "Revenant":
                sw.RevenantOffset += amount;
                break;
            case "Avenger":
                sw.AvengerOffset += amount;
                break;
            case "Crusader":
                sw.CrusaderOffset += amount;
                break;
            case "Protector":
                sw.ProtectorOffset += amount;
                break;
            case "Thunderlord":
                sw.ThunderlordOffset += amount;
                break;
            case "Spiritguard":
                sw.SpiritguardOffset += amount;
                break;
            case "Earthwarden":
                sw.EarthwardenOffset += amount;
                break;
            case "Assassin":
                sw.AssassinOffset += amount;
                break;
            case "Vindicator":
                sw.VindicatorOffset += amount;
                break;
            case "Apothecary":
                sw.ApothecaryOffset += amount;
                break;
            case "Conjurer":
                sw.ConjurerOffset += amount;
                break;
            case "Sentinel":
                sw.SentinelOffset += amount;
                break;
            case "Luminary":
                sw.LuminaryOffset += amount;
                break;
        }
    }
}
