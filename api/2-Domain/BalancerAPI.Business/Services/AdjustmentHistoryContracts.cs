namespace BalancerAPI.Business.Services;

public sealed record AdjustmentBaseHistoryResponse(
    string Name,
    Guid Uuid,
    IReadOnlyList<AdjustmentBaseHistoryEntry> Entries);

public sealed record AdjustmentBaseHistoryEntry(
    Guid Id,
    DateTime Date,
    string Source,
    int PreviousWeight,
    int NewWeight);

public sealed record AdjustmentSpecHistoryResponse(
    string Name,
    Guid Uuid,
    IReadOnlyList<AdjustmentSpecHistoryEntry> Entries);

public sealed record AdjustmentSpecHistoryEntry(
    Guid Id,
    DateTime Date,
    string Source,
    string Spec,
    int? WeekKey,
    int? Wins,
    int? Losses,
    int? Adjusted,
    int PreviousOffset,
    int NewOffset,
    int PreviousSpecWeight,
    int NewSpecWeight);
