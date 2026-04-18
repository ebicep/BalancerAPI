namespace BalancerAPI.Domain.Models;

/// <summary>
/// Snapshot of cumulative base weight per player for a <see cref="TimeWeek"/> id (<c>week_start_date</c>).
/// </summary>
public class BaseWeightWeekly
{
    public required Guid Uuid { get; set; }
    public int WeekStartDate { get; set; }
    public int Weight { get; set; }
}
