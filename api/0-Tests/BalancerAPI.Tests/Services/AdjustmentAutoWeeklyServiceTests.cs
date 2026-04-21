using BalancerAPI.Business.Services;
using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Tests.Services;

public class AdjustmentAutoWeeklyServiceTests
{
    private static readonly Guid U1 = Guid.Parse("d4e5f6a7-b8c9-0123-def0-123456789abc");
    private static readonly DateTime FixedLastUpdated = new(2025, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(1, 0, 0)]
    [InlineData(2, 0, 0)]
    [InlineData(2, 1, 0)]
    [InlineData(3, 0, 1)]
    [InlineData(4, 0, 2)]
    [InlineData(5, 1, 2)]
    [InlineData(10, 3, 5)]
    [InlineData(0, 5, -3)]
    [InlineData(0, 2, 0)]
    [InlineData(1, 3, 0)]
    [InlineData(0, 3, -1)]
    [InlineData(0, 4, -2)]
    [InlineData(1, 5, -2)]
    [InlineData(0, 10, -8)]
    [InlineData(3, 10, -5)]
    public void ComputeWeeklySpecOffsetAdjustment_MatchesNetThresholds(int wins, int losses, int expected)
    {
        Assert.Equal(expected, AdjustmentAutoWeeklyService.ComputeWeeklySpecOffsetAdjustment(wins, losses));
    }

    [Fact]
    public async Task ApplyAutoWeeklyAsync_WhenSpecAdjusted_WritesAdjustmentWeeklyLog()
    {
        await using var db = CreateDbContext();
        db.TimeWeeks.AddRange(
            new TimeWeek { Id = 4, Timestamp = DateTime.UtcNow.AddDays(-7) },
            new TimeWeek { Id = 5, Timestamp = DateTime.UtcNow });
        db.ExperimentalSpecsWlCurrentWeek.Add(new ExperimentalSpecsWlCurrentWeek
        {
            Uuid = U1,
            PyromancerWins = 4,
            PyromancerLosses = 0
        });
        db.BaseWeights.Add(new BaseWeight { Uuid = U1, Weight = 100, LastUpdated = FixedLastUpdated });
        db.ExperimentalSpecWeights.Add(new ExperimentalSpecWeight
        {
            Uuid = U1,
            PyromancerOffset = 5,
            LastUpdated = FixedLastUpdated
        });
        await db.SaveChangesAsync();

        var before = DateTime.UtcNow;
        var service = new AdjustmentAutoWeeklyService(db);
        var result = await service.ApplyAutoWeeklyAsync(CancellationToken.None);
        var after = DateTime.UtcNow;

        Assert.Equal(1, result.Count);
        var adjusted = Assert.Single(result.Adjusted);
        Assert.Equal(U1, adjusted.Key);
        var spec = Assert.Single(adjusted.Value.Specs);
        Assert.Equal("Pyromancer", spec.Spec);
        Assert.Equal(95, spec.PreviousWeight);
        Assert.Equal(97, spec.CurrentWeight);
        Assert.Equal(5, spec.PreviousOffset);
        Assert.Equal(3, spec.CurrentOffset);

        var log = Assert.Single(await db.AdjustmentWeeklyLogs.ToListAsync());
        Assert.Equal(5, log.WeekKey);
        Assert.Equal(U1, log.Uuid);
        Assert.Equal("Pyromancer", log.Spec);
        Assert.Equal(4, log.Wins);
        Assert.Equal(0, log.Losses);
        Assert.Equal(2, log.Adjusted);
        Assert.Equal(95, log.PreviousWeight);
        Assert.Equal(5, log.PreviousOffset);
        Assert.True(log.Date >= before && log.Date <= after);
    }

    [Fact]
    public async Task ApplyAutoWeeklyAsync_WhenNoAdjustments_DoesNotWriteLogs()
    {
        await using var db = CreateDbContext();
        db.TimeWeeks.Add(new TimeWeek { Id = 9, Timestamp = DateTime.UtcNow });
        db.ExperimentalSpecsWlCurrentWeek.Add(new ExperimentalSpecsWlCurrentWeek
        {
            Uuid = U1,
            PyromancerWins = 2,
            PyromancerLosses = 0
        });
        db.BaseWeights.Add(new BaseWeight { Uuid = U1, Weight = 100, LastUpdated = FixedLastUpdated });
        db.ExperimentalSpecWeights.Add(new ExperimentalSpecWeight
        {
            Uuid = U1,
            PyromancerOffset = 5,
            LastUpdated = FixedLastUpdated
        });
        await db.SaveChangesAsync();

        var service = new AdjustmentAutoWeeklyService(db);
        var result = await service.ApplyAutoWeeklyAsync(CancellationToken.None);

        Assert.Equal(0, result.Count);
        Assert.Empty(result.Adjusted);
        Assert.Empty(await db.AdjustmentWeeklyLogs.ToListAsync());
    }

    private static BalancerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BalancerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestBalancerDbContext(options);
    }

    private sealed class TestBalancerDbContext(DbContextOptions<BalancerDbContext> options) : BalancerDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Ignore<ExperimentalSpecsWlCurrentWeek>();
            modelBuilder.Entity<ExperimentalSpecsWlCurrentWeek>(entity =>
            {
                entity.ToTable("experimental_specs_wl_current_week_test");
                entity.HasKey(x => x.Uuid);
            });
        }
    }
}
