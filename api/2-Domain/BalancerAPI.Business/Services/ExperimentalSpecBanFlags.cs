using BalancerAPI.Domain.Models;

namespace BalancerAPI.Business.Services;

internal static class ExperimentalSpecBanFlags
{
    public static bool IsBanned(ExperimentalSpecBan? row, string spec)
    {
        if (row is null)
        {
            return false;
        }

        return GetBanFlag(row, spec);
    }

    public static bool GetBanFlag(ExperimentalSpecBan row, string spec) =>
        spec switch
        {
            "Pyromancer" => row.Pyromancer,
            "Cryomancer" => row.Cryomancer,
            "Aquamancer" => row.Aquamancer,
            "Berserker" => row.Berserker,
            "Defender" => row.Defender,
            "Revenant" => row.Revenant,
            "Avenger" => row.Avenger,
            "Crusader" => row.Crusader,
            "Protector" => row.Protector,
            "Thunderlord" => row.Thunderlord,
            "Spiritguard" => row.Spiritguard,
            "Earthwarden" => row.Earthwarden,
            "Assassin" => row.Assassin,
            "Vindicator" => row.Vindicator,
            "Apothecary" => row.Apothecary,
            "Conjurer" => row.Conjurer,
            "Sentinel" => row.Sentinel,
            "Luminary" => row.Luminary,
            _ => false
        };

    public static void SetBanFlag(ExperimentalSpecBan row, string spec, bool banned)
    {
        switch (spec)
        {
            case "Pyromancer": row.Pyromancer = banned; break;
            case "Cryomancer": row.Cryomancer = banned; break;
            case "Aquamancer": row.Aquamancer = banned; break;
            case "Berserker": row.Berserker = banned; break;
            case "Defender": row.Defender = banned; break;
            case "Revenant": row.Revenant = banned; break;
            case "Avenger": row.Avenger = banned; break;
            case "Crusader": row.Crusader = banned; break;
            case "Protector": row.Protector = banned; break;
            case "Thunderlord": row.Thunderlord = banned; break;
            case "Spiritguard": row.Spiritguard = banned; break;
            case "Earthwarden": row.Earthwarden = banned; break;
            case "Assassin": row.Assassin = banned; break;
            case "Vindicator": row.Vindicator = banned; break;
            case "Apothecary": row.Apothecary = banned; break;
            case "Conjurer": row.Conjurer = banned; break;
            case "Sentinel": row.Sentinel = banned; break;
            case "Luminary": row.Luminary = banned; break;
        }
    }
}
