namespace BalancerAPI.Business.Services;

public static class ExperimentalSpecs
{
    public const string Empty = "Empty";

    public static readonly string[] AllOrdered =
    [
        "Pyromancer", "Cryomancer", "Aquamancer", "Berserker", "Defender", "Revenant",
        "Avenger", "Crusader", "Protector", "Thunderlord", "Spiritguard", "Earthwarden",
        "Assassin", "Vindicator", "Apothecary", "Conjurer", "Sentinel", "Luminary"
    ];

    public static readonly string[] Damage =
    [
        "Berserker", "Pyromancer", "Avenger", "Thunderlord", "Assassin", "Conjurer"
    ];

    public static readonly string[] Tank =
    [
        "Cryomancer", "Defender", "Vindicator", "Crusader", "Sentinel"
    ];

    public static readonly string[] TankPicks =
    [
        "Cryomancer", "Vindicator", "Crusader"
    ];

    public static readonly string[] Heal =
    [
        "Aquamancer", "Revenant", "Protector", "Earthwarden", "Apothecary", "Luminary"
    ];

    public static readonly HashSet<string> DamageSet = new(Damage, StringComparer.Ordinal);
    public static readonly HashSet<string> TankSet = new(Tank, StringComparer.Ordinal);
    public static readonly HashSet<string> HealSet = new(Heal, StringComparer.Ordinal);

    public static Dictionary<int, (int Dmg, int Tank, int Heal, string[] Required)> BuildRoleCounts(
        string mainHealer,
        IReadOnlyList<string> tankPicks)
    {
        return new Dictionary<int, (int Dmg, int Tank, int Heal, string[] Required)>
        {
            [6] = (2, 0, 1, ["Avenger", tankPicks[0], mainHealer]),
            [7] = (2, 0, 1, ["Avenger", tankPicks[0], tankPicks[1], mainHealer]),
            [8] = (2, 2, 1, ["Avenger", "Defender", mainHealer]),
            [9] = (2, 2, 2, ["Avenger", "Defender", mainHealer]),
            [10] = (3, 2, 2, ["Avenger", "Defender", mainHealer]),
            [11] = (3, 3, 2, ["Avenger", "Defender", mainHealer]),
            [12] = (3, 3, 2, ["Avenger", "Defender", "Earthwarden", mainHealer]),
            [13] = (4, 3, 3, ["Avenger", "Defender", mainHealer]),
            [14] = (4, 4, 2, ["Avenger", "Defender", "Aquamancer", "Luminary"])
        };
    }
}
