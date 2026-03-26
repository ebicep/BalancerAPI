using BalancerAPI.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Services;

public sealed record NewDayResponse(int NewDay);

public sealed record NewWeekResponse(int NewWeek);

public sealed class TimeService(BalancerDbContext dbContext) : ITimeService
{
    public async Task<int> CreateNewDayAsync(CancellationToken cancellationToken)
    {
        var maxId = await dbContext.TimeDays
            .Select(x => (int?)x.Id)
            .MaxAsync(cancellationToken);

        var newId = (maxId ?? -1) + 1;
        var timestamp = EasternTime.Now;

        dbContext.TimeDays.Add(new TimeDay
        {
            Id = newId,
            Timestamp = timestamp
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return newId;
    }

    public async Task<int> CreateNewWeekAsync(CancellationToken cancellationToken)
    {
        var maxId = await dbContext.TimeWeeks
            .Select(x => (int?)x.Id)
            .MaxAsync(cancellationToken);

        var newId = (maxId ?? -1) + 1;
        var timestamp = EasternTime.Now;

        dbContext.TimeWeeks.Add(new TimeWeek
        {
            Id = newId,
            Timestamp = timestamp
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return newId;
    }
}

internal static class EasternTime
{
    internal static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);

    private static readonly TimeZoneInfo Zone = ResolveEasternTimeZone();

    private static TimeZoneInfo ResolveEasternTimeZone()
    {
        foreach (var id in new[] { "Eastern Standard Time", "America/New_York" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
                // try next id (Windows vs IANA)
            }
            catch (InvalidTimeZoneException)
            {
                // try next id
            }
        }

        throw new InvalidOperationException(
            "Could not resolve Eastern time zone. Tried 'Eastern Standard Time' and 'America/New_York'.");
    }
}
