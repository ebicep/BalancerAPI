namespace BalancerAPI.Domain.Models;

/// <summary>
/// Each row represents a game/match. Each spec column holds the UUID of the player who played that spec.
/// </summary>
public class ExperimentalSpecLog
{
    public Guid Id { get; set; }

    public Guid? Pyromancer { get; set; }
    public Guid? Cryomancer { get; set; }
    public Guid? Aquamancer { get; set; }
    public Guid? Berserker { get; set; }
    public Guid? Defender { get; set; }
    public Guid? Revenant { get; set; }
    public Guid? Avenger { get; set; }
    public Guid? Crusader { get; set; }
    public Guid? Protector { get; set; }
    public Guid? Thunderlord { get; set; }
    public Guid? Spiritguard { get; set; }
    public Guid? Earthwarden { get; set; }
    public Guid? Assassin { get; set; }
    public Guid? Vindicator { get; set; }
    public Guid? Apothecary { get; set; }
    public Guid? Conjurer { get; set; }
    public Guid? Sentinel { get; set; }
    public Guid? Luminary { get; set; }
}

