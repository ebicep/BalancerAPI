using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Business.Services;

public interface ISettingsService
{
    Task<IReadOnlyDictionary<string, decimal>> GetAllAsync(CancellationToken cancellationToken);
    Task<SettingEntry?> GetByKeyAsync(string key, CancellationToken cancellationToken);
    Task<SettingEntry> UpsertAsync(string key, decimal value, CancellationToken cancellationToken);
}

public sealed class SettingsService(BalancerDbContext dbContext) : ISettingsService
{
    public async Task<IReadOnlyDictionary<string, decimal>> GetAllAsync(CancellationToken cancellationToken)
    {
        var rows = await dbContext.Settings
            .AsNoTracking()
            .OrderBy(x => x.Key)
            .Select(x => new { x.Key, x.Value })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
    }

    public async Task<SettingEntry?> GetByKeyAsync(string key, CancellationToken cancellationToken)
    {
        var row = await dbContext.Settings
            .AsNoTracking()
            .Where(x => x.Key == key)
            .Select(x => new SettingEntry(x.Key, x.Value, x.DisplayName))
            .FirstOrDefaultAsync(cancellationToken);

        return row;
    }

    public async Task<SettingEntry> UpsertAsync(string key, decimal value, CancellationToken cancellationToken)
    {
        var row = await dbContext.Settings
            .FirstOrDefaultAsync(x => x.Key == key, cancellationToken);

        if (row is null)
        {
            row = new Setting
            {
                Key = key,
                Value = value,
                DisplayName = null
            };
            dbContext.Settings.Add(row);
        }
        else
        {
            row.Value = value;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new SettingEntry(row.Key, row.Value, row.DisplayName);
    }
}

public sealed record SettingEntry(string Key, decimal Value, string? DisplayName);
public sealed record SettingsResponse(IReadOnlyDictionary<string, decimal> Data);
public sealed record SettingResponseData(string Key, decimal Value, string? DisplayName);
public sealed record SettingResponse(SettingResponseData Data);
public sealed record UpdateSettingRequest(decimal Value);
