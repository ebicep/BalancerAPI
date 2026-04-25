namespace BalancerAPI.Domain.Models;

/// <summary>
/// Audit row for each per-spec offset change applied by manual weekly adjustment.
/// </summary>
public class AdjustmentManualWeeklyLog
{
    public Guid Id { get; set; }

    /// <summary>Player uuid.</summary>
    public Guid Uuid { get; set; }

    public required string Spec { get; set; }

    public int PreviousOffset { get; set; }

    public int NewOffset { get; set; }

    public int BaseWeight { get; set; }

    public int PreviousSpecWeight { get; set; }

    public int NewSpecWeight { get; set; }

    /// <summary>UTC time the adjustment was recorded.</summary>
    public DateTime Date { get; set; }
}
