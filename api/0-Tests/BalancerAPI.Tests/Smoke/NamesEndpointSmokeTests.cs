using System.Net.Http.Json;
using BalancerAPI.Business.Services;
using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
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

        var dbRoot = new InMemoryDatabaseRoot();
        var databaseName = $"smoke-{Guid.NewGuid()}";
        var app = _factory.WithWebHostBuilder(builder =>
        {
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
            await db.SaveChangesAsync();
        }

        var client = app.CreateClient();
        using var response = await client.PostAsync("/names/update", content: null);
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