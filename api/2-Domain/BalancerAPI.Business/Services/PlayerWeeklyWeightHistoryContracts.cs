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
    SpecWeightsResponse SpecWeights);
