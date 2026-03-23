namespace BalancerAPI.Data.Models;

/// <summary>
/// Each row represents a game/match. Each spec column holds the UUID of the player who played that spec.
/// </summary>
public class ExperimentalSpecLog
{
    public Guid Id { get; set; }

    public string? Pyromancer { get; set; }
    public string? Cryomancer { get; set; }
    public string? Aquamancer { get; set; }
    public string? Berserker { get; set; }
    public string? Defender { get; set; }
    public string? Revenant { get; set; }
    public string? Avenger { get; set; }
    public string? Crusader { get; set; }
    public string? Protector { get; set; }
    public string? Thunderlord { get; set; }
    public string? Spiritguard { get; set; }
    public string? Earthwarden { get; set; }
    public string? Assassin { get; set; }
    public string? Vindicator { get; set; }
    public string? Apothecary { get; set; }
    public string? Conjurer { get; set; }
    public string? Sentinel { get; set; }
    public string? Luminary { get; set; }
}
