namespace BalancerAPI.Domain.Models;

/// <summary>
/// Per-player win/loss streak trajectory for the current day; cleared when a new day is created.
/// </summary>
public class AdjustmentDaily
{
    public required Guid Uuid { get; set; }

    public int Trajectory { get; set; }
}
