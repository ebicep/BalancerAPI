namespace BalancerAPI.Business.Services;

public interface ITimeService
{
    Task<int> CreateNewDayAsync(CancellationToken cancellationToken);
    Task<bool> UndoDayAsync(int dayId, CancellationToken cancellationToken);

    Task<NewWeekResponse> CreateNewWeekAsync(CancellationToken cancellationToken);
    Task<bool> UndoWeekAsync(int weekId, CancellationToken cancellationToken);

    Task<(int Season, DateTime Timestamp)> CreateNewSeasonAsync(CancellationToken cancellationToken);
    Task<bool> UndoSeasonAsync(int seasonId, CancellationToken cancellationToken);

    Task<(int Season, DateTime Timestamp)?> GetLatestSeasonAsync(CancellationToken cancellationToken);
}