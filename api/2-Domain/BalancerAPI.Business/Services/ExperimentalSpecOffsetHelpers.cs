using BalancerAPI.Domain.Models;

namespace BalancerAPI.Business.Services;

internal static class ExperimentalSpecOffsetHelpers
{
    public static int GetOffset(ExperimentalSpecWeight sw, string spec) =>
        spec switch
        {
            "Pyromancer" => sw.PyromancerOffset,
            "Cryomancer" => sw.CryomancerOffset,
            "Aquamancer" => sw.AquamancerOffset,
            "Berserker" => sw.BerserkerOffset,
            "Defender" => sw.DefenderOffset,
            "Revenant" => sw.RevenantOffset,
            "Avenger" => sw.AvengerOffset,
            "Crusader" => sw.CrusaderOffset,
            "Protector" => sw.ProtectorOffset,
            "Thunderlord" => sw.ThunderlordOffset,
            "Spiritguard" => sw.SpiritguardOffset,
            "Earthwarden" => sw.EarthwardenOffset,
            "Assassin" => sw.AssassinOffset,
            "Vindicator" => sw.VindicatorOffset,
            "Apothecary" => sw.ApothecaryOffset,
            "Conjurer" => sw.ConjurerOffset,
            "Sentinel" => sw.SentinelOffset,
            "Luminary" => sw.LuminaryOffset,
            _ => 0
        };

    /// <summary>Apply <c>offset -= adjustment</c> (winning week lowers offset; losing week raises it).</summary>
    public static void ApplyOffsetAdjustment(ExperimentalSpecWeight sw, string spec, int adjustment)
    {
        switch (spec)
        {
            case "Pyromancer":
                sw.PyromancerOffset -= adjustment;
                break;
            case "Cryomancer":
                sw.CryomancerOffset -= adjustment;
                break;
            case "Aquamancer":
                sw.AquamancerOffset -= adjustment;
                break;
            case "Berserker":
                sw.BerserkerOffset -= adjustment;
                break;
            case "Defender":
                sw.DefenderOffset -= adjustment;
                break;
            case "Revenant":
                sw.RevenantOffset -= adjustment;
                break;
            case "Avenger":
                sw.AvengerOffset -= adjustment;
                break;
            case "Crusader":
                sw.CrusaderOffset -= adjustment;
                break;
            case "Protector":
                sw.ProtectorOffset -= adjustment;
                break;
            case "Thunderlord":
                sw.ThunderlordOffset -= adjustment;
                break;
            case "Spiritguard":
                sw.SpiritguardOffset -= adjustment;
                break;
            case "Earthwarden":
                sw.EarthwardenOffset -= adjustment;
                break;
            case "Assassin":
                sw.AssassinOffset -= adjustment;
                break;
            case "Vindicator":
                sw.VindicatorOffset -= adjustment;
                break;
            case "Apothecary":
                sw.ApothecaryOffset -= adjustment;
                break;
            case "Conjurer":
                sw.ConjurerOffset -= adjustment;
                break;
            case "Sentinel":
                sw.SentinelOffset -= adjustment;
                break;
            case "Luminary":
                sw.LuminaryOffset -= adjustment;
                break;
        }
    }

    public static void SetOffset(ExperimentalSpecWeight sw, string spec, int offset)
    {
        switch (spec)
        {
            case "Pyromancer":
                sw.PyromancerOffset = offset;
                break;
            case "Cryomancer":
                sw.CryomancerOffset = offset;
                break;
            case "Aquamancer":
                sw.AquamancerOffset = offset;
                break;
            case "Berserker":
                sw.BerserkerOffset = offset;
                break;
            case "Defender":
                sw.DefenderOffset = offset;
                break;
            case "Revenant":
                sw.RevenantOffset = offset;
                break;
            case "Avenger":
                sw.AvengerOffset = offset;
                break;
            case "Crusader":
                sw.CrusaderOffset = offset;
                break;
            case "Protector":
                sw.ProtectorOffset = offset;
                break;
            case "Thunderlord":
                sw.ThunderlordOffset = offset;
                break;
            case "Spiritguard":
                sw.SpiritguardOffset = offset;
                break;
            case "Earthwarden":
                sw.EarthwardenOffset = offset;
                break;
            case "Assassin":
                sw.AssassinOffset = offset;
                break;
            case "Vindicator":
                sw.VindicatorOffset = offset;
                break;
            case "Apothecary":
                sw.ApothecaryOffset = offset;
                break;
            case "Conjurer":
                sw.ConjurerOffset = offset;
                break;
            case "Sentinel":
                sw.SentinelOffset = offset;
                break;
            case "Luminary":
                sw.LuminaryOffset = offset;
                break;
        }
    }
}
