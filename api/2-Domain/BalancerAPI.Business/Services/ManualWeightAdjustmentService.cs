using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Business.Services;

public sealed class ManualWeightAdjustmentService(
    BalancerDbContext dbContext,
    IPlayerKeyResolver playerKeyResolver) : IManualWeightAdjustmentService
{
    public async Task<ManualWeightAdjustServiceResult<ManualBaseAdjustResponse>> PatchBaseAsync(
        string playerKey,
        ManualAdjustBaseRequest body,
        CancellationToken cancellationToken)
    {
        var resolved = await playerKeyResolver.ResolveAsync(playerKey, cancellationToken);
        if (!resolved.Success || resolved.Uuid is null)
        {
            return ManualWeightAdjustServiceResult<ManualBaseAdjustResponse>.Fail(
                resolved.StatusCode,
                resolved.Message!);
        }

        var uuid = resolved.Uuid.Value;
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
        baseWeight.Weight = body.Set ? body.Amount : baseWeight.Weight + body.Amount;
        var adjustmentDaily = await dbContext.AdjustmentDaily
            .AsTracking()
            .FirstOrDefaultAsync(x => x.Uuid == uuid, cancellationToken);
        var previousTrajectory = adjustmentDaily?.Trajectory ?? 0;
        if (adjustmentDaily is not null)
        {
            adjustmentDaily.Trajectory = 0;
        }
        var newTrajectory = adjustmentDaily?.Trajectory ?? 0;
        var recordedAt = DateTime.UtcNow;
        var response = new ManualBaseAdjustResponse(
            uuid,
            displayName,
            previousWeight,
            baseWeight.Weight,
            previousTrajectory,
            newTrajectory);
        dbContext.AdjustmentManualDailyLogs.Add(new AdjustmentManualDailyLog
        {
            Id = Guid.NewGuid(),
            Uuid = uuid,
            PreviousWeight = response.PreviousWeight,
            NewWeight = response.NewWeight,
            Date = recordedAt
        });
        await SnapshotGuard.EnsureBaseWeightDailyAsync(dbContext, [uuid], cancellationToken);
        await SnapshotGuard.EnsureBaseWeightWeeklyAsync(dbContext, [uuid], cancellationToken);
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

        var resolved = await playerKeyResolver.ResolveAsync(playerKey, cancellationToken);
        if (!resolved.Success || resolved.Uuid is null)
        {
            return ManualWeightAdjustServiceResult<ManualSpecAdjustResponse>.Fail(
                resolved.StatusCode,
                resolved.Message!);
        }

        var uuid = resolved.Uuid.Value;
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
        if (body.Set)
        {
            SetOffset(specRow, canonicalSpec, ClampOffset(baseWeight.Weight - body.Amount));
        }
        else
        {
            AddToOffset(specRow, canonicalSpec, body.Amount);
        }
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
        await SnapshotGuard.EnsureSpecWeightsWeeklyAsync(dbContext, [uuid], cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ManualWeightAdjustServiceResult<ManualSpecAdjustResponse>.Ok(response);
    }

    public static string? TryNormalizeSpec(string? spec)
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

    private static void AddToOffset(ExperimentalSpecWeight sw, string spec, int amount) =>
        SetOffset(sw, spec, ClampOffset(GetOffset(sw, spec) + amount));

    private static void SetOffset(ExperimentalSpecWeight sw, string spec, int offset)
    {
        switch (spec)
        {
            case "Pyromancer":
                sw.PyromancerOffset = offset;
                break;
            case "Cryomancer":
                sw.CryomancerOffset = offset;
                break;
            case "Aquamancer":
                sw.AquamancerOffset = offset;
                break;
            case "Berserker":
                sw.BerserkerOffset = offset;
                break;
            case "Defender":
                sw.DefenderOffset = offset;
                break;
            case "Revenant":
                sw.RevenantOffset = offset;
                break;
            case "Avenger":
                sw.AvengerOffset = offset;
                break;
            case "Crusader":
                sw.CrusaderOffset = offset;
                break;
            case "Protector":
                sw.ProtectorOffset = offset;
                break;
            case "Thunderlord":
                sw.ThunderlordOffset = offset;
                break;
            case "Spiritguard":
                sw.SpiritguardOffset = offset;
                break;
            case "Earthwarden":
                sw.EarthwardenOffset = offset;
                break;
            case "Assassin":
                sw.AssassinOffset = offset;
                break;
            case "Vindicator":
                sw.VindicatorOffset = offset;
                break;
            case "Apothecary":
                sw.ApothecaryOffset = offset;
                break;
            case "Conjurer":
                sw.ConjurerOffset = offset;
                break;
            case "Sentinel":
                sw.SentinelOffset = offset;
                break;
            case "Luminary":
                sw.LuminaryOffset = offset;
                break;
        }
    }

    private static int ClampOffset(int candidate) =>
        candidate is > 10000 or < -10000
            ? 10000
            : candidate;
}
