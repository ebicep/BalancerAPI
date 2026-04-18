namespace BalancerAPI.Domain.Models;

/// <summary>
/// Snapshot of cumulative base weight per player for a <see cref="TimeDay"/> id (<c>day_start_date</c>).
/// </summary>
public class BaseWeightDaily
{
    public required Guid Uuid { get; set; }
    public int DayStartDate { get; set; }
    public int Weight { get; set; }
}
