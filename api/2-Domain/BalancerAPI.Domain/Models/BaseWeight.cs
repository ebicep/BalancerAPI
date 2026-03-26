namespace BalancerAPI.Domain.Models;

public class BaseWeight
{
    public required Guid Uuid { get; set; }
    public int Weight { get; set; }
}
