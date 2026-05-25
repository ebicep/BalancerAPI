namespace BalancerAPI.Domain.Models;

public class ExperimentalSpecRequest
{
    public required Guid Uuid { get; set; }

    public required string Spec { get; set; }

    public int GameCooldown { get; set; }

    public DateTime CreatedTime { get; set; }
}
