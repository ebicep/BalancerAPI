namespace BalancerAPI.Domain.Models;

/// <summary>
/// Result shape for <c>experimental_season_stats_season</c> view: per-player W/L/K/D totals for a completed season,
/// aggregated from <see cref="ExperimentalSpecsWlSeason"/>.
/// </summary>
public class ExperimentalSeasonStatsSeason
{
    public int SeasonStartDate { get; set; }
    public required Guid Uuid { get; set; }

    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
}