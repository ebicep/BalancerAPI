namespace BalancerAPI.Business.Services;

public interface ITimeService
{
    Task<int> CreateNewDayAsync(CancellationToken cancellationToken);

    Task<int> CreateNewWeekAsync(CancellationToken cancellationToken);
}