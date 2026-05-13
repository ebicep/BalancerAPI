namespace BalancerAPI.Domain.Models;

/// <summary>
/// Per-player experimental spec ban flags (column order matches <c>ExperimentalSpecs.AllOrdered</c>).
/// </summary>
public class ExperimentalSpecBan
{
    public required Guid Uuid { get; set; }

    public bool Pyromancer { get; set; }
    public bool Cryomancer { get; set; }
    public bool Aquamancer { get; set; }
    public bool Berserker { get; set; }
    public bool Defender { get; set; }
    public bool Revenant { get; set; }
    public bool Avenger { get; set; }
    public bool Crusader { get; set; }
    public bool Protector { get; set; }
    public bool Thunderlord { get; set; }
    public bool Spiritguard { get; set; }
    public bool Earthwarden { get; set; }
    public bool Assassin { get; set; }
    public bool Vindicator { get; set; }
    public bool Apothecary { get; set; }
    public bool Conjurer { get; set; }
    public bool Sentinel { get; set; }
    public bool Luminary { get; set; }
}
