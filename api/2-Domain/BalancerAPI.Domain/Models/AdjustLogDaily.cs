namespace BalancerAPI.Domain.Models;

public class AdjustLogDaily
{
    public required Guid Uuid { get; set; }
    public int Adjustment { get; set; }
}
