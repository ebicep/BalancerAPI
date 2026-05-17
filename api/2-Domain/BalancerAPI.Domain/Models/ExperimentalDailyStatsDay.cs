namespace BalancerAPI.Domain.Models;

/// <summary>
/// Result shape for <c>experimental_daily_stats_day</c> view: per-player W/L/K/D totals for a completed calendar day,
/// aggregated from <see cref="ExperimentalSpecsWlDay"/>.
/// </summary>
public class ExperimentalDailyStatsDay
{
    public int DayStartDate { get; set; }
    public required Guid Uuid { get; set; }

    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
}
