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

    public static readonly IReadOnlyDictionary<string, int> KbValues =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["Berserker"]   = 3,
            ["Revenant"]    = 3,
            ["Earthwarden"] = 3,
            ["Pyromancer"]  = 2,
            ["Crusader"]    = 2,
            ["Thunderlord"] = 2,
        };

    private static readonly IReadOnlyDictionary<string, string[]> ByClass =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [ExperimentalClasses.Mage] =
                ["Pyromancer", "Cryomancer", "Aquamancer"],
            [ExperimentalClasses.Warrior] =
                ["Berserker", "Defender", "Revenant"],
            [ExperimentalClasses.Paladin] =
                ["Avenger", "Crusader", "Protector"],
            [ExperimentalClasses.Shaman] =
                ["Thunderlord", "Spiritguard", "Earthwarden"],
            [ExperimentalClasses.Rogue] =
                ["Assassin", "Vindicator", "Apothecary"],
            [ExperimentalClasses.Arcanist] =
                ["Conjurer", "Sentinel", "Luminary"],
        };

    private static readonly IReadOnlyDictionary<string, string[]> BySpecType =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [ExperimentalSpecTypes.Damage] = Damage,
            [ExperimentalSpecTypes.Tank] = [..Tank, "Spiritguard"],
            [ExperimentalSpecTypes.Healer] = Heal,
        };

    public static string? TryNormalizeClass(string? className)
    {
        if (string.IsNullOrWhiteSpace(className))
        {
            return null;
        }

        var trimmed = className.Trim();
        return ExperimentalClasses.AllOrdered.FirstOrDefault(
            c => string.Equals(c, trimmed, StringComparison.OrdinalIgnoreCase));
    }

    public static string? TryNormalizeSpecType(string? specType)
    {
        if (string.IsNullOrWhiteSpace(specType))
        {
            return null;
        }

        var trimmed = specType.Trim();
        if (string.Equals(trimmed, "Heal", StringComparison.OrdinalIgnoreCase))
        {
            return ExperimentalSpecTypes.Healer;
        }

        return ExperimentalSpecTypes.AllOrdered.FirstOrDefault(
            t => string.Equals(t, trimmed, StringComparison.OrdinalIgnoreCase));
    }

    public static IReadOnlyList<string> SpecsForClass(string canonicalClass) =>
        SpecsInAllOrdered(ByClass[canonicalClass]);

    public static IReadOnlyList<string> SpecsForSpecType(string canonicalSpecType) =>
        SpecsInAllOrdered(BySpecType[canonicalSpecType]);

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
            [10] = (3, 2, 2, ["Avenger", "Defender", "Aquamancer"]),
            [11] = (3, 3, 2, ["Avenger", "Defender", "Aquamancer"]),
            [12] = (3, 3, 2, ["Avenger", "Defender", "Earthwarden", "Aquamancer"]),
            [13] = (4, 3, 3, ["Avenger", "Defender", "Aquamancer"]),
            [14] = (4, 4, 2, ["Avenger", "Defender", "Aquamancer", "Luminary"])
        };
    }

    private static IReadOnlyList<string> SpecsInAllOrdered(IReadOnlyCollection<string> specs)
    {
        var set = specs as HashSet<string> ?? new HashSet<string>(specs, StringComparer.Ordinal);
        return AllOrdered.Where(set.Contains).ToArray();
    }
}

public static class ExperimentalClasses
{
    public const string Mage = "Mage";
    public const string Warrior = "Warrior";
    public const string Paladin = "Paladin";
    public const string Shaman = "Shaman";
    public const string Rogue = "Rogue";
    public const string Arcanist = "Arcanist";

    public static readonly string[] AllOrdered =
    [
        Mage, Warrior, Paladin, Shaman, Rogue, Arcanist
    ];
}

public static class ExperimentalSpecTypes
{
    public const string Damage = "Damage";
    public const string Tank = "Tank";
    public const string Healer = "Healer";

    public static readonly string[] AllOrdered =
    [
        Damage, Tank, Healer
    ];
}
