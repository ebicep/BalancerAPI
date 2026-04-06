namespace BalancerAPI.Domain.Models;

public class ExperimentalBalanceLog
{
    public Guid BalanceId { get; set; }

    /// <summary>MongoDB ObjectId as 24 hex chars. Null until the result is inputted via the input endpoint.</summary>
    public string? GameId { get; set; }

    /// <summary>JSON array of teams (same shape as API <c>balance</c>).</summary>
    public string Balance { get; set; } = null!;

    /// <summary>JSON object (same shape as API <c>meta</c>).</summary>
    public string Meta { get; set; } = null!;

    /// <summary>Matches <c>meta.time</c> from the balance API response (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    public bool Posted { get; set; }

    public bool Inputted { get; set; }
}
