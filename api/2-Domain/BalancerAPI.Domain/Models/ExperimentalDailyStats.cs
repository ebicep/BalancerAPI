namespace BalancerAPI.Domain.Models;

/// <summary>
/// Database view <c>experimental_daily_stats</c>: per-player totals for the current day, summing all spec win/loss/kill/death columns from <see cref="ExperimentalSpecsWlCurrentDay"/>.
/// </summary>
public class ExperimentalDailyStats
{
    public required Guid Uuid { get; set; }

    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
}
