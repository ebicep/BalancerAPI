using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BalancerAPI.Api.Authentication;
using BalancerAPI.Common.Auth;
using BalancerAPI.Common.Security;
using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BalancerAPI.Tests.Auth;

public sealed class ApiAuthIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Pepper = "integration-pepper-aaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string AltPepper = "integration-pepper-bbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private const string Secret = "integration-secret-aaaaaaaaaaaaaaaaaaaaaaaa";
    private const string AltSecret = "integration-secret-bbbbbbbbbbbbbbbbbbbbbbbb";

    private readonly WebApplicationFactory<Program> _factory;

    public ApiAuthIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetSettings_WithoutAuth_Returns401()
    {
        using var app = CreateFactory(Pepper, NewDbName(), new InMemoryDatabaseRoot());
        var client = app.CreateClient();
        var response = await client.GetAsync("/api/v1/settings");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("problem+json", response.Content.Headers.ContentType?.MediaType ?? "", StringComparison.Ordinal);
        Assert.Equal(
            ApiKeyAuthenticationHandler.DefaultUnauthorizedDetail,
            await ReadProblemDetailAsync(response));
    }

    [Fact]
    public async Task GetSettings_WithReadOnlyKey_Returns200()
    {
        var clientId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        using var app = CreateFactory(Pepper, NewDbName(), new InMemoryDatabaseRoot());

        await SeedClientAsync(app, clientId, Secret, Pepper, pepperVersion: 1, ApiRoles.WebReadOnly);

        var client = app.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ApiKeyFormat.BuildKey(clientId, Secret));

        var response = await client.GetAsync("/api/v1/settings");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostSettings_WithReadOnlyKey_Returns403()
    {
        var clientId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        using var app = CreateFactory(Pepper, NewDbName(), new InMemoryDatabaseRoot());

        await SeedClientAsync(app, clientId, Secret, Pepper, pepperVersion: 1, ApiRoles.WebReadOnly);

        var client = app.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ApiKeyFormat.BuildKey(clientId, Secret));

        var response = await client.PostAsJsonAsync("/api/v1/settings/winLoss", new { value = 1m });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("problem+json", response.Content.Headers.ContentType?.MediaType ?? "", StringComparison.Ordinal);
        Assert.Equal(
            ApiKeyAuthenticationHandler.DefaultForbiddenDetail,
            await ReadProblemDetailAsync(response));
    }

    [Fact]
    public async Task GetSettings_WithInvalidApiKeyFormat_Returns401WithDetail()
    {
        using var app = CreateFactory(Pepper, NewDbName(), new InMemoryDatabaseRoot());
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-bkr-key");

        var response = await client.GetAsync("/api/v1/settings");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("problem+json", response.Content.Headers.ContentType?.MediaType ?? "", StringComparison.Ordinal);
        Assert.Equal("Invalid API key format.", await ReadProblemDetailAsync(response));
    }

    [Fact]
    public async Task GetSettings_WithKeyHashedByPreviousPepper_Returns200()
    {
        var clientId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var dbRoot = new InMemoryDatabaseRoot();
        var dbName = NewDbName();

        // Server is running on the new pepper (version 2) but PreviousPeppers maps version 1 → old pepper.
        using var app = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:ApiKey:Pepper"] = Pepper,
                    ["Authentication:ApiKey:PepperVersion"] = "2",
                    ["Authentication:ApiKey:PreviousPeppers:1"] = AltPepper,
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost"
                });
            });
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDbContextOptionsConfiguration<BalancerDbContext>>();
                services.RemoveAll<DbContextOptions<BalancerDbContext>>();
                services.AddDbContext<BalancerDbContext>(o => o.UseInMemoryDatabase(dbName, dbRoot));
            });
        });

        // Existing key was hashed with the OLD pepper (AltPepper) when version was 1.
        await SeedClientAsync(app, clientId, AltSecret, AltPepper, pepperVersion: 1, ApiRoles.WebReadOnly);

        var client = app.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ApiKeyFormat.BuildKey(clientId, AltSecret));

        var response = await client.GetAsync("/api/v1/settings");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetSettings_WithRevokedKey_Returns401()
    {
        var clientId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        using var app = CreateFactory(Pepper, NewDbName(), new InMemoryDatabaseRoot());

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BalancerDbContext>();
            db.ApiClients.Add(new ApiClient
            {
                Id = clientId,
                Name = "revoked",
                SecretHash = ApiKeyHasher.HashSecret(Secret, Pepper),
                PepperVersion = 1,
                Roles = [ApiRoles.WebReadOnly],
                CreatedAt = DateTimeOffset.UtcNow,
                RevokedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var client = app.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ApiKeyFormat.BuildKey(clientId, Secret));

        var response = await client.GetAsync("/api/v1/settings");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SuccessfulAuth_StampsLastUsedAt()
    {
        var clientId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        using var app = CreateFactory(Pepper, NewDbName(), new InMemoryDatabaseRoot());

        await SeedClientAsync(app, clientId, Secret, Pepper, pepperVersion: 1, ApiRoles.WebReadOnly);

        var http = app.CreateClient();
        http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ApiKeyFormat.BuildKey(clientId, Secret));
        var response = await http.GetAsync("/api/v1/settings");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BalancerDbContext>();
        var row = await db.ApiClients.AsNoTracking().FirstAsync(c => c.Id == clientId);
        Assert.NotNull(row.LastUsedAt);
    }

    private static async Task SeedClientAsync(
        WebApplicationFactory<Program> app,
        Guid clientId,
        string secret,
        string pepper,
        int pepperVersion,
        string role)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BalancerDbContext>();
        db.ApiClients.Add(new ApiClient
        {
            Id = clientId,
            Name = "test-client",
            SecretHash = ApiKeyHasher.HashSecret(secret, pepper),
            PepperVersion = pepperVersion,
            Roles = [role],
            CreatedAt = DateTimeOffset.UtcNow,
            RevokedAt = null
        });
        await db.SaveChangesAsync();
    }

    private static string NewDbName() => $"auth-{Guid.NewGuid()}";

    private static async Task<string?> ReadProblemDetailAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        return doc.RootElement.TryGetProperty("detail", out var detail) ? detail.GetString() : null;
    }

    private WebApplicationFactory<Program> CreateFactory(string pepper, string databaseName, InMemoryDatabaseRoot dbRoot)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:ApiKey:Pepper"] = pepper,
                    ["Authentication:ApiKey:PepperVersion"] = "1",
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost"
                });
            });
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDbContextOptionsConfiguration<BalancerDbContext>>();
                services.RemoveAll<DbContextOptions<BalancerDbContext>>();
                services.AddDbContext<BalancerDbContext>(options =>
                    options.UseInMemoryDatabase(databaseName, dbRoot));
            });
        });
    }
}
