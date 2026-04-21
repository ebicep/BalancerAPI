using BalancerAPI.Data.Data;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Business.Services;

public interface ISpecWeightsService
{
    Task<SpecWeightsResponse?> GetCombinedAsync(Guid uuid, CancellationToken cancellationToken);
}

public sealed class SpecWeightsService(BalancerDbContext dbContext) : ISpecWeightsService
{
    public async Task<SpecWeightsResponse?> GetCombinedAsync(Guid uuid, CancellationToken cancellationToken)
    {
        var row = await (
            from b in dbContext.BaseWeights.AsNoTracking()
            join specRow in dbContext.ExperimentalSpecWeights.AsNoTracking() on b.Uuid equals specRow.Uuid
            where b.Uuid == uuid
            select new { b.Weight, Spec = specRow }
        ).FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var baseWeight = row.Weight;
        var spec = row.Spec;
        return new SpecWeightsResponse(
            baseWeight - spec.PyromancerOffset,
            baseWeight - spec.CryomancerOffset,
            baseWeight - spec.AquamancerOffset,
            baseWeight - spec.BerserkerOffset,
            baseWeight - spec.DefenderOffset,
            baseWeight - spec.RevenantOffset,
            baseWeight - spec.AvengerOffset,
            baseWeight - spec.CrusaderOffset,
            baseWeight - spec.ProtectorOffset,
            baseWeight - spec.ThunderlordOffset,
            baseWeight - spec.SpiritguardOffset,
            baseWeight - spec.EarthwardenOffset,
            baseWeight - spec.AssassinOffset,
            baseWeight - spec.VindicatorOffset,
            baseWeight - spec.ApothecaryOffset,
            baseWeight - spec.ConjurerOffset,
            baseWeight - spec.SentinelOffset,
            baseWeight - spec.LuminaryOffset);
    }
}

public sealed record SpecWeightsResponse(
    int Pyromancer,
    int Cryomancer,
    int Aquamancer,
    int Berserker,
    int Defender,
    int Revenant,
    int Avenger,
    int Crusader,
    int Protector,
    int Thunderlord,
    int Spiritguard,
    int Earthwarden,
    int Assassin,
    int Vindicator,
    int Apothecary,
    int Conjurer,
    int Sentinel,
    int Luminary);