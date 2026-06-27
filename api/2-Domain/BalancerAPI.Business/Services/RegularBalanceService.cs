using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Business.Services;

public interface IRegularBalanceService
{
    Task<RegularBalanceServiceResult> BalanceAsync(
        RegularBalanceRequest request,
        CancellationToken cancellationToken);
}

public sealed class RegularBalanceService(
    IDbContextFactory<BalancerDbContext> dbContextFactory) : IRegularBalanceService
{
    public async Task<RegularBalanceServiceResult> BalanceAsync(
        RegularBalanceRequest request,
        CancellationToken cancellationToken)
    {
        var requestStopwatch = System.Diagnostics.Stopwatch.StartNew();
        var players = request.Players.ToList();
        if (players.Count == 0)
            return Fail(400, "players must not be empty.");
        if (players.Count % 2 != 0)
            return Fail(400, "players count must be even.");
        var teamSize = players.Count / 2;
        if (teamSize is < 6 or > 14)
            return Fail(400, $"team size must be between 6 and 14 (got {teamSize}).");
        var distinct = players.Distinct().ToList();
        if (distinct.Count != players.Count)
            return Fail(400, "duplicate player UUIDs are not allowed.");

        var steps = new List<ExperimentalBalanceMetaStep>(3);
        var dataFetchStartOffset = requestStopwatch.Elapsed.TotalMilliseconds;
        var dataFetchStopwatch = System.Diagnostics.Stopwatch.StartNew();

        var settingsTask = LoadSettingsAsync(cancellationToken);
        var playerDataTask = LoadPlayerDataAsync(players, cancellationToken);
        await Task.WhenAll(settingsTask, playerDataTask);

        var settings = await settingsTask;
        var maxIter = GetIntSetting(settings, "max_balance_iterations", 500_000);
        var maxWeightDiff = GetIntSetting(settings, "max_weight_diff", 20);
        var maxWlDiff = GetIntSetting(settings, "max_wl_diff", 50);
        var maxKdDiff = GetIntSetting(settings, "max_kd_diff", 10);

        var playerData = await playerDataTask;
        if (playerData.Missing.Count > 0)
        {
            return new RegularBalanceServiceResult(
                false, null,
                new RegularBalanceError(404, "One or more players are missing base weights or experimental spec weights.", playerData.Missing));
        }

        dataFetchStopwatch.Stop();
        steps.Add(new ExperimentalBalanceMetaStep(
            Name: "db.query.playerData",
            DurationMs: dataFetchStopwatch.Elapsed.TotalMilliseconds,
            StartOffsetMs: dataFetchStartOffset));

        var random = Random.Shared;
        var computeStartOffset = requestStopwatch.Elapsed.TotalMilliseconds;
        var sw = System.Diagnostics.Stopwatch.StartNew();

        for (var iter = 0; iter < maxIter; iter++)
        {
            ShuffleInPlace(players, random);

            var blue = players.Take(teamSize).ToArray();
            var red = players.Skip(teamSize).Take(teamSize).ToArray();

            var blueWeights = blue.Select(p => playerData.AvgWeightByPlayer[p]).ToArray();
            var redWeights = red.Select(p => playerData.AvgWeightByPlayer[p]).ToArray();

            ApplySmallTeamDiscount(teamSize, blueWeights);
            ApplySmallTeamDiscount(teamSize, redWeights);

            var bw = blueWeights.Sum();
            var rw = redWeights.Sum();
            if (Math.Abs(bw - rw) > maxWeightDiff) continue;

            var blueWl = blue.Sum(p => playerData.PlayerDataByPlayer[p].DailyWinLoss);
            var redWl = red.Sum(p => playerData.PlayerDataByPlayer[p].DailyWinLoss);
            if (Math.Abs(blueWl - redWl) > maxWlDiff) continue;

            var blueKd = blue.Sum(p => playerData.PlayerDataByPlayer[p].GlobalNetKdPerGame);
            var redKd = red.Sum(p => playerData.PlayerDataByPlayer[p].GlobalNetKdPerGame);
            if (!(Math.Abs(blueKd - redKd) <= maxKdDiff)) continue;

            sw.Stop();
            steps.Add(new ExperimentalBalanceMetaStep(
                Name: "algorithm.computeBalance",
                DurationMs: sw.Elapsed.TotalMilliseconds,
                StartOffsetMs: computeStartOffset));

            var serializeStartOffset = requestStopwatch.Elapsed.TotalMilliseconds;
            var serializeStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var teamBalance = BuildTeamBalance(blue, blueWeights, red, redWeights, playerData);
            serializeStopwatch.Stop();
            steps.Add(new ExperimentalBalanceMetaStep(
                Name: "response.serialize",
                DurationMs: serializeStopwatch.Elapsed.TotalMilliseconds,
                StartOffsetMs: serializeStartOffset));

            var latestSeason = await LoadLatestSeasonAsync(cancellationToken);
            var meta = new ExperimentalBalanceMeta(
                Iterations: iter + 1,
                DurationMs: requestStopwatch.Elapsed.TotalMilliseconds,
                Steps: steps,
                Season: latestSeason?.Id ?? 0,
                Time: DateTime.UtcNow);

            var response = new RegularBalanceResponse(Guid.NewGuid(), teamBalance, meta);
            return new RegularBalanceServiceResult(true, response, null);
        }

        sw.Stop();
        return new RegularBalanceServiceResult(
            false, null,
            new RegularBalanceError(409, "Could not find balanced teams after exhausting all iterations."));
    }

    private static RegularBalanceServiceResult Fail(int status, string message) =>
        new(false, null, new RegularBalanceError(status, message));

    private static IReadOnlyList<RegularBalanceTeam> BuildTeamBalance(
        Guid[] blue, int[] blueWeights,
        Guid[] red, int[] redWeights,
        PlayerDataResult data)
    {
        return [BuildTeam(blue, blueWeights, data), BuildTeam(red, redWeights, data)];
    }

    private static RegularBalanceTeam BuildTeam(Guid[] playerIds, int[] evalWeights, PlayerDataResult data)
    {
        var players = new List<RegularBalancePlayer>(playerIds.Length);
        var totalWeight = 0;
        var totalWinLoss = 0;
        var totalNetKdPerGame = 0.0;

        for (var i = 0; i < playerIds.Length; i++)
        {
            var pid = playerIds[i];
            var row = data.PlayerDataByPlayer[pid];
            var name = data.NamesByPlayer.TryGetValue(pid, out var n) ? n : string.Empty;
            var p = new RegularBalancePlayer(
                Uuid: pid,
                Name: name,
                Weight: evalWeights[i],
                Talker: 0,
                WinLoss: row.DailyWinLoss,
                NetKdPerGame: row.GlobalNetKdPerGame);
            players.Add(p);
            totalWeight += evalWeights[i];
            totalWinLoss += row.DailyWinLoss;
            totalNetKdPerGame += row.GlobalNetKdPerGame;
        }

        return new RegularBalanceTeam(
            TotalWeight: totalWeight,
            TotalTalkers: 0,
            TotalWinLoss: totalWinLoss,
            TotalNetKdPerGame: totalNetKdPerGame,
            Players: players);
    }

    private static void ApplySmallTeamDiscount(int teamSize, int[] weights)
    {
        if (teamSize > 8) return;
        var total = weights.Sum();
        if (total == 0) return;
        for (var i = 0; i < weights.Length; i++)
        {
            if (weights[i] / (double)total > 0.3)
                weights[i] = (int)Math.Round(weights[i] * 0.8, MidpointRounding.AwayFromZero);
        }
    }

    private static void ShuffleInPlace<T>(IList<T> list, Random random)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static int GetIntSetting(IReadOnlyDictionary<string, decimal> settings, string key, int defaultValue) =>
        settings.TryGetValue(key, out var v) ? (int)Math.Round(v, MidpointRounding.AwayFromZero) : defaultValue;

    private sealed record LatestSeasonInfo(int Id, DateTime Timestamp);
    private sealed record PlayerDataResult(
        Dictionary<Guid, int> AvgWeightByPlayer,
        Dictionary<Guid, ExperimentalBalancePlayerData> PlayerDataByPlayer,
        Dictionary<Guid, string> NamesByPlayer,
        List<Guid> Missing);

    private async Task<IReadOnlyDictionary<string, decimal>> LoadSettingsAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.Settings
            .AsNoTracking()
            .OrderBy(x => x.Key)
            .Select(x => new { x.Key, x.Value })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
    }

    private async Task<LatestSeasonInfo?> LoadLatestSeasonAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.TimeSeasons
            .AsNoTracking()
            .OrderByDescending(x => x.Id)
            .Select(x => new LatestSeasonInfo(x.Id, x.Timestamp))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<PlayerDataResult> LoadPlayerDataAsync(
        IReadOnlyList<Guid> players,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.ExperimentalBalancePlayerData
            .AsNoTracking()
            .Where(x => players.Contains(x.Uuid))
            .ToListAsync(cancellationToken);

        var avgWeights = new Dictionary<Guid, int>();
        var dataByPlayer = new Dictionary<Guid, ExperimentalBalancePlayerData>();
        var namesByPlayer = new Dictionary<Guid, string>();

        foreach (var row in rows)
        {
            var vec = BuildWeightVector(row);
            avgWeights[row.Uuid] = (int)Math.Round(vec.Average(), MidpointRounding.AwayFromZero);
            dataByPlayer[row.Uuid] = row;
            namesByPlayer[row.Uuid] = row.Name;
        }

        var missing = players.Where(p => !avgWeights.ContainsKey(p)).ToList();
        return new PlayerDataResult(avgWeights, dataByPlayer, namesByPlayer, missing);
    }

    private static int[] BuildWeightVector(ExperimentalBalancePlayerData row) =>
    [
        row.PyromancerWeight, row.CryomancerWeight, row.AquamancerWeight,
        row.BerserkerWeight, row.DefenderWeight, row.RevenantWeight,
        row.AvengerWeight, row.CrusaderWeight, row.ProtectorWeight,
        row.ThunderlordWeight, row.SpiritguardWeight, row.EarthwardenWeight,
        row.AssassinWeight, row.VindicatorWeight, row.ApothecaryWeight,
        row.ConjurerWeight, row.SentinelWeight, row.LuminaryWeight
    ];
}
