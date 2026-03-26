namespace BalancerAPI.Domain.Models;

public class PlayerName
{
    public required Guid Uuid { get; set; }
    public required string Name { get; set; }
    public string[] PreviousNames { get; set; } = [];
}
