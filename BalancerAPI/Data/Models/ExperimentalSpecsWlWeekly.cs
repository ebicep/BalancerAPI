namespace BalancerAPI.Data.Models;

/// <summary>
/// Weekly snapshot of cumulative win/loss/kill/death stats per player per spec.
/// </summary>
public class ExperimentalSpecsWlWeekly
{
    public required Guid Uuid { get; set; }
    public int WeekStartDate { get; set; }

    public int PyromancerWins { get; set; }
    public int PyromancerLosses { get; set; }
    public int PyromancerKills { get; set; }
    public int PyromancerDeaths { get; set; }

    public int CryomancerWins { get; set; }
    public int CryomancerLosses { get; set; }
    public int CryomancerKills { get; set; }
    public int CryomancerDeaths { get; set; }

    public int AquamancerWins { get; set; }
    public int AquamancerLosses { get; set; }
    public int AquamancerKills { get; set; }
    public int AquamancerDeaths { get; set; }

    public int BerserkerWins { get; set; }
    public int BerserkerLosses { get; set; }
    public int BerserkerKills { get; set; }
    public int BerserkerDeaths { get; set; }

    public int DefenderWins { get; set; }
    public int DefenderLosses { get; set; }
    public int DefenderKills { get; set; }
    public int DefenderDeaths { get; set; }

    public int RevenantWins { get; set; }
    public int RevenantLosses { get; set; }
    public int RevenantKills { get; set; }
    public int RevenantDeaths { get; set; }

    public int AvengerWins { get; set; }
    public int AvengerLosses { get; set; }
    public int AvengerKills { get; set; }
    public int AvengerDeaths { get; set; }

    public int CrusaderWins { get; set; }
    public int CrusaderLosses { get; set; }
    public int CrusaderKills { get; set; }
    public int CrusaderDeaths { get; set; }

    public int ProtectorWins { get; set; }
    public int ProtectorLosses { get; set; }
    public int ProtectorKills { get; set; }
    public int ProtectorDeaths { get; set; }

    public int ThunderlordWins { get; set; }
    public int ThunderlordLosses { get; set; }
    public int ThunderlordKills { get; set; }
    public int ThunderlordDeaths { get; set; }

    public int SpiritguardWins { get; set; }
    public int SpiritguardLosses { get; set; }
    public int SpiritguardKills { get; set; }
    public int SpiritguardDeaths { get; set; }

    public int EarthwardenWins { get; set; }
    public int EarthwardenLosses { get; set; }
    public int EarthwardenKills { get; set; }
    public int EarthwardenDeaths { get; set; }

    public int AssassinWins { get; set; }
    public int AssassinLosses { get; set; }
    public int AssassinKills { get; set; }
    public int AssassinDeaths { get; set; }

    public int VindicatorWins { get; set; }
    public int VindicatorLosses { get; set; }
    public int VindicatorKills { get; set; }
    public int VindicatorDeaths { get; set; }

    public int ApothecaryWins { get; set; }
    public int ApothecaryLosses { get; set; }
    public int ApothecaryKills { get; set; }
    public int ApothecaryDeaths { get; set; }

    public int ConjurerWins { get; set; }
    public int ConjurerLosses { get; set; }
    public int ConjurerKills { get; set; }
    public int ConjurerDeaths { get; set; }

    public int SentinelWins { get; set; }
    public int SentinelLosses { get; set; }
    public int SentinelKills { get; set; }
    public int SentinelDeaths { get; set; }

    public int LuminaryWins { get; set; }
    public int LuminaryLosses { get; set; }
    public int LuminaryKills { get; set; }
    public int LuminaryDeaths { get; set; }
}
