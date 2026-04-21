namespace BalancerAPI.Domain.Models;

/// <summary>
/// Audit row for each per-spec offset change applied by auto-weekly adjustment.
/// </summary>
public class AdjustmentWeeklyLog
{
    public Guid Id { get; set; }

    /// <summary>Week key matching weekly snapshot week_start_date values.</summary>
    public int WeekKey { get; set; }

    /// <summary>Player uuid.</summary>
    public Guid Uuid { get; set; }

    public required string Spec { get; set; }

    public int Wins { get; set; }

    public int Losses { get; set; }

    public int Adjusted { get; set; }

    public int PreviousWeight { get; set; }

    public int PreviousOffset { get; set; }

    /// <summary>UTC time the adjustment was recorded.</summary>
    public DateTime Date { get; set; }
}
