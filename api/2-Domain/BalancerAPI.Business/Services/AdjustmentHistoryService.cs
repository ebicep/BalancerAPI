using BalancerAPI.Data.Data;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Business.Services;

public interface IAdjustmentHistoryService
{
    Task<AdjustmentBaseHistoryResponse?> GetBaseHistoryAsync(Guid uuid, CancellationToken cancellationToken);

    Task<AdjustmentSpecHistoryResponse?> GetSpecHistoryAsync(Guid uuid, CancellationToken cancellationToken);
}

public sealed class AdjustmentHistoryService(BalancerDbContext dbContext) : IAdjustmentHistoryService
{
    private const string SourceAuto = "auto";
    private const string SourceManual = "manual";

    public async Task<AdjustmentBaseHistoryResponse?> GetBaseHistoryAsync(
        Guid uuid,
        CancellationToken cancellationToken)
    {
        var hasBaseWeight = await dbContext.BaseWeights.AsNoTracking()
            .AnyAsync(x => x.Uuid == uuid, cancellationToken);
        if (!hasBaseWeight)
        {
            return null;
        }

        var displayName = await dbContext.Names.AsNoTracking()
            .Where(n => n.Uuid == uuid)
            .Select(n => n.Name)
            .OrderBy(n => n)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var autoLogs = await dbContext.AdjustmentDailyLogs.AsNoTracking()
            .Where(x => x.Uuid == uuid)
            .Select(x => new AdjustmentBaseHistoryEntry(
                x.Id,
                x.Date,
                SourceAuto,
                x.PreviousWeight,
                x.NewWeight))
            .ToListAsync(cancellationToken);

        var manualLogs = await dbContext.AdjustmentManualDailyLogs.AsNoTracking()
            .Where(x => x.Uuid == uuid)
            .Select(x => new AdjustmentBaseHistoryEntry(
                x.Id,
                x.Date,
                SourceManual,
                x.PreviousWeight,
                x.NewWeight))
            .ToListAsync(cancellationToken);

        var entries = autoLogs
            .Concat(manualLogs)
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.Id)
            .ToList();

        return new AdjustmentBaseHistoryResponse(displayName, uuid, entries);
    }

    public async Task<AdjustmentSpecHistoryResponse?> GetSpecHistoryAsync(
        Guid uuid,
        CancellationToken cancellationToken)
    {
        var hasSpecRow = await dbContext.ExperimentalSpecWeights.AsNoTracking()
            .AnyAsync(x => x.Uuid == uuid, cancellationToken);
        if (!hasSpecRow)
        {
            return null;
        }

        var displayName = await dbContext.Names.AsNoTracking()
            .Where(n => n.Uuid == uuid)
            .Select(n => n.Name)
            .OrderBy(n => n)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var autoRows = await dbContext.AdjustmentWeeklyLogs.AsNoTracking()
            .Where(x => x.Uuid == uuid)
            .ToListAsync(cancellationToken);

        var autoEntries = autoRows.Select(x =>
        {
            var newOffset = x.PreviousOffset - x.Adjusted;
            var newSpecWeight = x.PreviousWeight + x.Adjusted;
            return new AdjustmentSpecHistoryEntry(
                x.Id,
                x.Date,
                SourceAuto,
                x.Spec,
                x.WeekKey,
                x.Wins,
                x.Losses,
                x.Adjusted,
                x.PreviousOffset,
                newOffset,
                x.PreviousWeight,
                newSpecWeight);
        });

        var manualEntries = await dbContext.AdjustmentManualWeeklyLogs.AsNoTracking()
            .Where(x => x.Uuid == uuid)
            .Select(x => new AdjustmentSpecHistoryEntry(
                x.Id,
                x.Date,
                SourceManual,
                x.Spec,
                null,
                null,
                null,
                null,
                x.PreviousOffset,
                x.NewOffset,
                x.PreviousSpecWeight,
                x.NewSpecWeight))
            .ToListAsync(cancellationToken);

        var entries = autoEntries
            .Concat(manualEntries)
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.Id)
            .ToList();

        return new AdjustmentSpecHistoryResponse(displayName, uuid, entries);
    }
}
