namespace BalancerAPI.Domain.Models;

public class Setting
{
    public string Key { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string? DisplayName { get; set; }
}
