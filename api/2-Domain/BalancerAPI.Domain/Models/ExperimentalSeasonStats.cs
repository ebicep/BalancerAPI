namespace BalancerAPI.Domain.Models;

/// <summary>
/// Database view <c>experimental_season_stats</c>: per-player totals for the current season, summing all spec win/loss/kill/death columns from <see cref="ExperimentalSpecsWlCurrentSeason"/>.
/// </summary>
public class ExperimentalSeasonStats
{
    public required Guid Uuid { get; set; }

    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
}
