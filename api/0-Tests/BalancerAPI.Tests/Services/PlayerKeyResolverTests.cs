using BalancerAPI.Business.Services;
using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Tests.Services;

public class PlayerKeyResolverTests
{
    private static readonly Guid U1 = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    private static readonly Guid U2 = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901");

    [Fact]
    public async Task ResolveAsync_ByUuid_ReturnsUuid()
    {
        var (resolver, db) = CreateResolver();
        await using (db)
        {
            db.Names.Add(new PlayerName { Uuid = U1, Name = "Amy" });
            await db.SaveChangesAsync();

            var result = await resolver.ResolveAsync(U1.ToString(), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(U1, result.Uuid);
            Assert.Equal("Amy", result.DisplayName);
        }
    }

    [Fact]
    public async Task ResolveAsync_ByName_ReturnsUuidAndDisplayName()
    {
        var (resolver, db) = CreateResolver();
        await using (db)
        {
            db.Names.Add(new PlayerName { Uuid = U1, Name = "Amy" });
            await db.SaveChangesAsync();

            var result = await resolver.ResolveAsync("amy", CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(U1, result.Uuid);
            Assert.Equal("Amy", result.DisplayName);
        }
    }

    [Fact]
    public async Task ResolveAsync_WhenEmpty_Returns400()
    {
        var (resolver, db) = CreateResolver();
        await using (db)
        {
            var result = await resolver.ResolveAsync("   ", CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal(400, result.StatusCode);
            Assert.Contains("required", result.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task ResolveAsync_WhenUnknownName_Returns404()
    {
        var (resolver, db) = CreateResolver();
        await using (db)
        {
            var result = await resolver.ResolveAsync("nobody", CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal(404, result.StatusCode);
            Assert.Contains("nobody", result.Message);
        }
    }

    [Fact]
    public async Task ResolveAsync_WhenAmbiguousName_Returns409()
    {
        var (resolver, db) = CreateResolver();
        await using (db)
        {
            db.Names.Add(new PlayerName { Uuid = U1, Name = "Amy" });
            db.Names.Add(new PlayerName { Uuid = U2, Name = "Amy" });
            await db.SaveChangesAsync();

            var result = await resolver.ResolveAsync("Amy", CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal(409, result.StatusCode);
            Assert.Contains("ambiguous", result.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static (PlayerKeyResolver Resolver, BalancerDbContext Db) CreateResolver()
    {
        var options = new DbContextOptionsBuilder<BalancerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new BalancerDbContext(options);
        return (new PlayerKeyResolver(db), db);
    }
}
