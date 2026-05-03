using System.Security.Claims;
using System.Text.Encodings.Web;
using BalancerAPI.Api.Security;
using BalancerAPI.Common.Auth;
using BalancerAPI.Common.Security;
using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace BalancerAPI.Api.Authentication;

public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> schemeOptions,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IDbContextFactory<BalancerDbContext> dbFactory,
    IOptionsMonitor<ApiKeyOptions> apiKeyOptions,
    IMemoryCache cache)
    : AuthenticationHandler<AuthenticationSchemeOptions>(schemeOptions, logger, encoder)
{
    public const string SchemeName = "ApiKey";

    /// <summary>Default <see cref="ProblemDetails.Detail"/> when no Authorization header, wrong scheme, or empty bearer token.</summary>
    public const string DefaultUnauthorizedDetail =
        "Authentication required. Provide a valid Bearer API key.";

    /// <summary>Default <see cref="ProblemDetails.Detail"/> for authenticated clients that lack permission.</summary>
    public const string DefaultForbiddenDetail =
        "You do not have permission to perform this operation.";

    private static readonly object ChallengeDetailItemKey = new();

    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan LastUsedDebounce = TimeSpan.FromMinutes(1);

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authValues))
        {
            return AuthenticateResult.NoResult();
        }

        var raw = authValues.ToString();
        const string bearerPrefix = "Bearer ";
        if (!raw.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var token = raw.AsSpan(bearerPrefix.Length).Trim().ToString();
        if (token.Length == 0)
        {
            return AuthFail("Bearer token is empty.");
        }

        if (!ApiKeyFormat.TryParse(token, out var publicId, out var secret))
        {
            return AuthFail("Invalid API key format.");
        }

        var snapshot = await GetClientSnapshotAsync(publicId, Context.RequestAborted);
        if (snapshot is null || snapshot.RevokedAt is not null)
        {
            return AuthFail("Invalid API key.");
        }

        if (!apiKeyOptions.CurrentValue.TryGetPepper(snapshot.PepperVersion, out var pepper))
        {
            // Misconfigured server (pepper for this row's version is not loaded).
            Logger.LogError(
                "No pepper configured for ApiClient {ClientId} pepper_version={Version}.",
                snapshot.Id, snapshot.PepperVersion);
            return AuthFail("Invalid API key.");
        }

        var expectedHash = ApiKeyHasher.HashSecret(secret, pepper);
        if (!ApiKeyHasher.FixedTimeEquals(expectedHash, snapshot.SecretHash))
        {
            return AuthFail("Invalid API key.");
        }

        if (!RolePermissions.TryResolvePermissions(snapshot.Roles, out var permissions))
        {
            return AuthFail("Unknown or invalid API role on client.");
        }

        await TouchLastUsedAsync(snapshot, Context.RequestAborted);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, snapshot.Id.ToString()),
            new(ApiClaims.ClientName, snapshot.Name)
        };

        foreach (var r in snapshot.Roles)
        {
            claims.Add(new Claim(ApiClaims.ApiRole, r));
        }

        foreach (var p in permissions!)
        {
            claims.Add(new Claim(ApiClaims.Permission, p));
        }

        Logger.LogInformation(
            "API key authenticated: client={ClientName} id={ClientId}", snapshot.Name, snapshot.Id);

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName));
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties? properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        var detail = Context.Items.TryGetValue(ChallengeDetailItemKey, out var v) && v is string s
            ? s
            : DefaultUnauthorizedDetail;
        await Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Title = "Unauthorized",
                Status = StatusCodes.Status401Unauthorized,
                Detail = detail
            },
            options: null,
            contentType: "application/problem+json");
    }

    protected override async Task HandleForbiddenAsync(AuthenticationProperties? properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        await Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Title = "Forbidden",
                Status = StatusCodes.Status403Forbidden,
                Detail = DefaultForbiddenDetail
            },
            options: null,
            contentType: "application/problem+json");
    }

    private AuthenticateResult AuthFail(string message)
    {
        Context.Items[ChallengeDetailItemKey] = message;
        return AuthenticateResult.Fail(message);
    }

    private async Task<CachedApiClient?> GetClientSnapshotAsync(Guid publicId, CancellationToken ct)
    {
        if (cache.TryGetValue<CachedApiClient>(CacheKey(publicId), out var cached) && cached is not null)
        {
            return cached;
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.ApiClients.AsNoTracking()
            .Where(c => c.Id == publicId)
            .Select(c => new CachedApiClient(
                c.Id, c.Name, c.SecretHash, c.PepperVersion, c.Roles, c.RevokedAt, c.LastUsedAt))
            .FirstOrDefaultAsync(ct);
        if (row is null)
        {
            return null;
        }

        cache.Set(CacheKey(publicId), row, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheTtl,
            Size = 1
        });
        return row;
    }

    private async Task TouchLastUsedAsync(CachedApiClient snapshot, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        if (snapshot.LastUsedAt is { } last && now - last < LastUsedDebounce)
        {
            return;
        }

        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var stub = new ApiClient
            {
                Id = snapshot.Id,
                Name = snapshot.Name,
                SecretHash = snapshot.SecretHash,
                PepperVersion = snapshot.PepperVersion,
                Roles = snapshot.Roles,
                RevokedAt = snapshot.RevokedAt,
                LastUsedAt = now
            };
            db.ApiClients.Attach(stub);
            db.Entry(stub).Property(e => e.LastUsedAt).IsModified = true;
            await db.SaveChangesAsync(ct);

            cache.Set(CacheKey(snapshot.Id), snapshot with { LastUsedAt = now }, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheTtl,
                Size = 1
            });
        }
        catch (OperationCanceledException)
        {
            // Request aborted; ignore.
        }
        catch (Exception ex)
        {
            // last_used_at is best-effort; never fail auth because of it.
            Logger.LogDebug(ex, "Failed to update last_used_at for ApiClient {ClientId}.", snapshot.Id);
        }
    }

    private static string CacheKey(Guid publicId) => $"apikey:{publicId:D}";

    private sealed record CachedApiClient(
        Guid Id,
        string Name,
        byte[] SecretHash,
        int PepperVersion,
        string[] Roles,
        DateTimeOffset? RevokedAt,
        DateTimeOffset? LastUsedAt);
}
