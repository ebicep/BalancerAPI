namespace BalancerAPI.Business.Services;

public interface ITimeService
{
    Task<int> CreateNewDayAsync(CancellationToken cancellationToken);

    Task<int> CreateNewWeekAsync(CancellationToken cancellationToken);

    Task<(int Season, DateTime Timestamp)> CreateNewSeasonAsync(CancellationToken cancellationToken);

    Task<(int Season, DateTime Timestamp)?> GetLatestSeasonAsync(CancellationToken cancellationToken);
}