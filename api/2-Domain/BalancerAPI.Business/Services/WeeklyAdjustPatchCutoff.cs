namespace BalancerAPI.Business.Services;

public static class WeeklyAdjustPatchCutoff
{
    /// <summary>2026-09-27 02:00 EST — auto/manual weekly logs before this instant are eligible.</summary>
    public static DateTime Utc => new(2026, 9, 27, 7, 0, 0, DateTimeKind.Utc);
}
