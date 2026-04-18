namespace BalancerAPI.Domain.Models;

/// <summary>
/// Database view: live base weight minus the snapshot for the previous <see cref="TimeWeek"/> id
/// (second-highest id in <c>time_week</c>), or zero delta if there is no prior week or no matching history row.
/// </summary>
public class BaseWeightCurrentWeek
{
    public required Guid Uuid { get; set; }
    public int CurrentWeight { get; set; }
    public int? PreviousWeight { get; set; }
    public int? WeekStartDate { get; set; }
    public int WeightChange { get; set; }
}
