using System.Net.Http.Headers;
using System.Net.Http.Json;
using BalancerAPI.Business.Services;
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

namespace BalancerAPI.Tests.Smoke;

public sealed class NamesEndpointSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public NamesEndpointSmokeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task PostUpdate_WhenSmokeEnabled_CallsMojangAndReturnsUpdatedName()
    {
        if (!IsSmokeEnabled())
        {
            return;
        }

        const string pepper = "smoke-test-pepper-aaaaaaaaaaaaaaaaaaaaaaaaaa";
        var apiClientId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        const string apiSecret = "smoke-secret-key-material-aaaaaaaaaaaaaaaaa";

        var dbRoot = new InMemoryDatabaseRoot();
        var databaseName = $"smoke-{Guid.NewGuid()}";
        var app = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:ApiKey:Pepper"] = pepper,
                    ["Authentication:ApiKey:PepperVersion"] = "1"
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

        var knownUuid = Guid.Parse("9f2b2230-3b2c-4b0f-a141-d7b598e236c7");
        const string expectedCurrentName = "sumSmash";
        const string seededOldName = "oldName";

        using (var seedScope = app.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<BalancerDbContext>();
            db.Names.Add(new PlayerName
            {
                Uuid = knownUuid,
                Name = seededOldName,
                PreviousNames = []
            });
            db.ApiClients.Add(new ApiClient
            {
                Id = apiClientId,
                Name = "smoke",
                SecretHash = ApiKeyHasher.HashSecret(apiSecret, pepper),
                PepperVersion = 1,
                Roles = [ApiRoles.BotFull],
                CreatedAt = DateTimeOffset.UtcNow,
                RevokedAt = null
            });
            await db.SaveChangesAsync();
        }

        var client = app.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ApiKeyFormat.BuildKey(apiClientId, apiSecret));
        using var response = await client.PostAsync("/api/v1/names/update", content: null);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<UpdateNamesResponse>();
        Assert.NotNull(payload);

        var update = Assert.Single(payload.Updated);
        Assert.Equal(knownUuid, update.Uuid);
        Assert.Equal(seededOldName, update.Previous);
        Assert.Equal(expectedCurrentName, update.Current);

        using var verifyScope = app.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<BalancerDbContext>();
        var row = verifyDb.Names.Single(x => x.Uuid == knownUuid);
        Assert.Equal(expectedCurrentName, row.Name);
        Assert.Contains(seededOldName, row.PreviousNames);
    }

    private static bool IsSmokeEnabled()
    {
        return string.Equals(Environment.GetEnvironmentVariable("RUN_SMOKE_TESTS"), "1", StringComparison.Ordinal);
    }
}
