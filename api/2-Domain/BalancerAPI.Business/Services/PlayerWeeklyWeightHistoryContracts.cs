namespace BalancerAPI.Business.Services;

public sealed record PlayerWeeklyWeightHistoryResponse(
    string Name,
    Guid Uuid,
    IReadOnlyList<WeeklyWeightPoint> Weeks);

public sealed record WeeklyWeightPoint(
    int WeekId,
    DateTime Timestamp,
    bool IsCurrentWeek,
    int BaseWeight,
    SpecOffsetsResponse SpecOffsets);

/// <summary>Per-spec offset values from <c>experimental_spec_weights_weekly</c> (not effective spec weight).</summary>
public sealed record SpecOffsetsResponse(
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
