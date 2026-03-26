namespace BalancerAPI.Domain.Models;

public class ExperimentalSpecWeight
{
    public required Guid Uuid { get; set; }

    public int PyromancerOffset { get; set; }
    public int CryomancerOffset { get; set; }
    public int AquamancerOffset { get; set; }
    public int BerserkerOffset { get; set; }
    public int DefenderOffset { get; set; }
    public int RevenantOffset { get; set; }
    public int AvengerOffset { get; set; }
    public int CrusaderOffset { get; set; }
    public int ProtectorOffset { get; set; }
    public int ThunderlordOffset { get; set; }
    public int SpiritguardOffset { get; set; }
    public int EarthwardenOffset { get; set; }
    public int AssassinOffset { get; set; }
    public int VindicatorOffset { get; set; }
    public int ApothecaryOffset { get; set; }
    public int ConjurerOffset { get; set; }
    public int SentinelOffset { get; set; }
    public int LuminaryOffset { get; set; }

    public DateTime LastUpdated { get; set; }
}