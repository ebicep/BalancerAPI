namespace BalancerAPI.Domain.Models;

public class ApiClient
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public byte[] SecretHash { get; set; } = [];

    /// <summary>Pepper version used to compute <see cref="SecretHash"/>. Allows rotating peppers without invalidating existing keys.</summary>
    public int PepperVersion { get; set; } = 1;

    public string[] Roles { get; set; } = [];
    public DateTimeOffset? RevokedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Best-effort last-authenticated timestamp; updated on successful auth, debounced.</summary>
    public DateTimeOffset? LastUsedAt { get; set; }
}
