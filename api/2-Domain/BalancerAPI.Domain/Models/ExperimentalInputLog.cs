namespace BalancerAPI.Domain.Models;

public class ExperimentalInputLog
{
    public Guid Id { get; set; }

    public Guid BalanceId { get; set; }

    /// <summary>MongoDB ObjectId as 24 hex chars (same as input request <c>game_id</c>).</summary>
    public string GameId { get; set; } = null!;

    /// <summary>Audit action, e.g. <c>input</c>, <c>uninput</c>, or <c>clear</c>.</summary>
    public string Action { get; set; } = null!;

    /// <summary>UTC time of the action (column <c>date</c>).</summary>
    public DateTime OccurredAt { get; set; }
}
