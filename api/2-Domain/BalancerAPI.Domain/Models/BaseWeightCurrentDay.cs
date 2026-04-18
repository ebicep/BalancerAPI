namespace BalancerAPI.Domain.Models;

/// <summary>
/// Database view: live base weight minus the snapshot for the previous <see cref="TimeDay"/> id
/// (second-highest id in <c>time_day</c>), or zero delta if there is no prior day or no matching history row.
/// </summary>
public class BaseWeightCurrentDay
{
    public required Guid Uuid { get; set; }
    public int CurrentWeight { get; set; }
    public int? PreviousWeight { get; set; }
    public int? DayStartDate { get; set; }
    public int WeightChange { get; set; }
}
