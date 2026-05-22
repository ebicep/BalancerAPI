using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Business.Services;

public sealed class AdjustmentAutoDailyService(BalancerDbContext dbContext) : IAdjustmentAutoDailyService
{
    /// <summary>Max allowed difference between request <c>date</c> and latest log batch (JSON/DB precision).</summary>
    internal static readonly TimeSpan UndoBatchDateTolerance = TimeSpan.FromMilliseconds(1);
    
    public async Task<AdjustmentAutoDailyResponse> ApplyAutoDailyAsync(CancellationToken cancellationToken)
    {
        var rows = await (
            from adj in dbContext.AdjustmentDaily
            where adj.Trajectory >= 3 || adj.Trajectory <= -3
            join bw in dbContext.BaseWeights on adj.Uuid equals bw.Uuid
            join n in dbContext.Names on adj.Uuid equals n.Uuid into nameJoin
            from n in nameJoin.DefaultIfEmpty()
            orderby adj.Uuid
            select new { Adjustment = adj, BaseWeight = bw, Name = n != null ? n.Name : null }
        ).ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return new AdjustmentAutoDailyResponse(0, [], null);
        }

        var adjusted = new List<AdjustmentAutoDailyAdjustedEntry>(rows.Count);
        var recordedAt = DateTime.UtcNow;

        foreach (var row in rows)
        {
            var adj = row.Adjustment;
            var baseWeight = row.BaseWeight;
            var previousTrajectory = adj.Trajectory;
            var d = ComputeDelta(adj.Trajectory);

            var previousWeight = baseWeight.Weight;
            baseWeight.Weight += d;
            var newTrajectory = d > 0 ? 2 : -2;
            adj.Trajectory = newTrajectory;

            dbContext.AdjustmentDailyLogs.Add(new AdjustmentDailyLog
            {
                Id = Guid.NewGuid(),
                Uuid = adj.Uuid,
                PreviousWeight = previousWeight,
                NewWeight = baseWeight.Weight,
                Date = recordedAt
            });

            adjusted.Add(new AdjustmentAutoDailyAdjustedEntry(
                adj.Uuid,
                row.Name ?? string.Empty,
                previousWeight,
                baseWeight.Weight,
                previousTrajectory,
                newTrajectory));
        }

        var uuids = rows.Select(r => r.Adjustment.Uuid).ToList();
        await SnapshotGuard.EnsureBaseWeightDailyAsync(dbContext, uuids, cancellationToken);
        await SnapshotGuard.EnsureBaseWeightWeeklyAsync(dbContext, uuids, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AdjustmentAutoDailyResponse(adjusted.Count, adjusted, recordedAt);
    }

    public async Task<AdjustmentAutoDailyUndoResult> UndoAutoDailyAsync(
        AdjustmentAutoDailyResponse request,
        CancellationToken cancellationToken)
    {
        if (request.Count == 0)
        {
            return AdjustmentAutoDailyUndoResult.Fail(400, "Nothing to undo.");
        }

        if (request.Date is null)
        {
            return AdjustmentAutoDailyUndoResult.Fail(400, "Date is required.");
        }

        var latestDate = await dbContext.AdjustmentDailyLogs
            .MaxAsync(x => (DateTime?)x.Date, cancellationToken);

        if (latestDate is null)
        {
            return AdjustmentAutoDailyUndoResult.Fail(
                409,
                "No auto-daily adjustment logs exist.");
        }

        var batchDate = latestDate.Value;
        if (!DatesWithinTolerance(request.Date.Value, batchDate, UndoBatchDateTolerance))
        {
            return AdjustmentAutoDailyUndoResult.Fail(
                409,
                "date must match the latest auto-daily adjustment batch.");
        }

        var logs = await dbContext.AdjustmentDailyLogs
            .Where(x => x.Date == batchDate)
            .ToListAsync(cancellationToken);

        if (logs.Count != request.Count)
        {
            return AdjustmentAutoDailyUndoResult.Fail(
                409,
                "Adjustment log count does not match the request.");
        }

        var logByUuid = logs.ToDictionary(x => x.Uuid);
        var requestUuids = request.Adjusted.Select(x => x.Uuid).ToHashSet();

        if (logByUuid.Count != requestUuids.Count || !requestUuids.All(logByUuid.ContainsKey))
        {
            return AdjustmentAutoDailyUndoResult.Fail(
                409,
                "Adjustment log players do not match the request.");
        }

        foreach (var entry in request.Adjusted)
        {
            var log = logByUuid[entry.Uuid];
            if (log.PreviousWeight != entry.PreviousWeight || log.NewWeight != entry.CurrentWeight)
            {
                return AdjustmentAutoDailyUndoResult.Fail(
                    409,
                    "Adjustment logs do not match the request payload.");
            }
        }

        var uuids = request.Adjusted.Select(x => x.Uuid).ToList();
        var baseWeights = await dbContext.BaseWeights
            .Where(x => uuids.Contains(x.Uuid))
            .ToDictionaryAsync(x => x.Uuid, cancellationToken);

        var adjustments = await dbContext.AdjustmentDaily
            .Where(x => uuids.Contains(x.Uuid))
            .ToDictionaryAsync(x => x.Uuid, cancellationToken);

        foreach (var entry in request.Adjusted)
        {
            if (!baseWeights.TryGetValue(entry.Uuid, out var baseWeight))
            {
                return AdjustmentAutoDailyUndoResult.Fail(
                    409,
                    $"Base weight row not found for player {entry.Uuid}.");
            }

            if (baseWeight.Weight != entry.CurrentWeight)
            {
                return AdjustmentAutoDailyUndoResult.Fail(
                    409,
                    "Current base weight no longer matches the auto-daily response.");
            }

            if (!adjustments.TryGetValue(entry.Uuid, out var adjustment))
            {
                return AdjustmentAutoDailyUndoResult.Fail(
                    409,
                    $"Adjustment daily row not found for player {entry.Uuid}.");
            }

            if (adjustment.Trajectory != entry.NewTrajectory)
            {
                return AdjustmentAutoDailyUndoResult.Fail(
                    409,
                    "Current trajectory no longer matches the auto-daily response.");
            }

            baseWeight.Weight = entry.PreviousWeight;
            adjustment.Trajectory = entry.PreviousTrajectory;
        }

        await SnapshotGuard.EnsureBaseWeightDailyAsync(dbContext, uuids, cancellationToken);
        await SnapshotGuard.EnsureBaseWeightWeeklyAsync(dbContext, uuids, cancellationToken);

        dbContext.AdjustmentDailyLogs.RemoveRange(logs);
        await dbContext.SaveChangesAsync(cancellationToken);

        var undone = request.Adjusted
            .Select(entry => new AdjustmentAutoDailyAdjustedEntry(
                entry.Uuid,
                entry.Name,
                entry.CurrentWeight,
                entry.PreviousWeight,
                entry.NewTrajectory,
                entry.PreviousTrajectory))
            .ToList();

        return AdjustmentAutoDailyUndoResult.Ok(
            new AdjustmentAutoDailyResponse(request.Count, undone, batchDate));
    }

    internal static bool DatesWithinTolerance(DateTime left, DateTime right, TimeSpan tolerance)
    {
        var delta = ToUtc(left) - ToUtc(right);
        return Math.Abs(delta.Ticks) <= tolerance.Ticks;
    }

    private static DateTime ToUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

    internal static int ComputeDelta(int trajectory) =>
        trajectory >= 3 ? trajectory - 2
        : trajectory <= -3 ? trajectory + 2
        : 0;
}
