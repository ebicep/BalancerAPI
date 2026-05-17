namespace BalancerAPI.Domain.Models;

/// <summary>
/// Result shape for <c>experimental_weekly_stats_week</c> view: per-player W/L/K/D totals for a completed calendar week,
/// aggregated from <see cref="ExperimentalSpecsWlWeek"/>.
/// </summary>
public class ExperimentalWeeklyStatsWeek
{
    public int WeekStartDate { get; set; }
    public required Guid Uuid { get; set; }

    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
}
