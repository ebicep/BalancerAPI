namespace BalancerAPI.Domain.Models;

/// <summary>
/// Audit row for each player weight change applied by manual daily adjustment.
/// </summary>
public class AdjustmentManualDailyLog
{
    public Guid Id { get; set; }

    /// <summary>Player uuid.</summary>
    public Guid Uuid { get; set; }

    public int PreviousWeight { get; set; }

    public int NewWeight { get; set; }

    /// <summary>UTC time the adjustment was recorded.</summary>
    public DateTime Date { get; set; }
}
