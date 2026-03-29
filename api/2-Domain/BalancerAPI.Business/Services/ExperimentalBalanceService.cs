using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Business.Services;

public interface IExperimentalBalanceService
{
    Task<ExperimentalBalanceServiceResult> BalanceAsync(
        ExperimentalBalanceRequest request,
        CancellationToken cancellationToken);
}

public sealed class ExperimentalBalanceService(
    BalancerDbContext dbContext,
    ISettingsService settingsService) : IExperimentalBalanceService
{
    private static readonly string[] AllSpecsOrdered =
    [
        "Pyromancer", "Cryomancer", "Aquamancer", "Berserker", "Defender", "Revenant",
        "Avenger", "Crusader", "Protector", "Thunderlord", "Spiritguard", "Earthwarden",
        "Assassin", "Vindicator", "Apothecary", "Conjurer", "Sentinel", "Luminary"
    ];

    private static readonly HashSet<string> DmgSpecs =
    [
        "Berserker", "Pyromancer", "Avenger", "Thunderlord", "Assassin", "Conjurer"
    ];

    private static readonly HashSet<string> TankSpecs =
    [
        "Cryomancer", "Defender", "Vindicator", "Crusader", "Sentinel"
    ];

    private static readonly HashSet<string> HealSpecs =
    [
        "Aquamancer", "Revenant", "Protector", "Earthwarden", "Apothecary", "Luminary"
    ];

    public async Task<ExperimentalBalanceServiceResult> BalanceAsync(
        ExperimentalBalanceRequest request,
        CancellationToken cancellationToken)
    {
        var requestStopwatch = System.Diagnostics.Stopwatch.StartNew();
        var players = request.Players?.ToList() ?? [];
        if (players.Count == 0)
        {
            return Fail(400, "players must not be empty.");
        }

        if (players.Count % 2 != 0)
        {
            return Fail(400, "players count must be even.");
        }

        var teamSize = players.Count / 2;
        if (teamSize is < 6 or > 14)
        {
            return Fail(400, $"team size must be between 6 and 14 (got {teamSize}).");
        }

        var distinct = players.Distinct().ToList();
        if (distinct.Count != players.Count)
        {
            return Fail(400, "duplicate player UUIDs are not allowed.");
        }

        var steps = new List<ExperimentalBalanceMetaStep>(3);
        var dataFetchStartOffset = requestStopwatch.Elapsed.TotalMilliseconds;
        var dataFetchStopwatch = System.Diagnostics.Stopwatch.StartNew();

        var settings = await settingsService.GetAllAsync(cancellationToken);
        var maxIter = GetIntSetting(settings, "max_balance_iterations", 500_000);
        var shuffleEvery = GetIntSetting(settings, "shuffle_specs_every_iterations", 50_000);
        var maxWeightDiff = GetIntSetting(settings, "max_weight_diff", 20);
        var maxFlatTeamDiff = GetIntSetting(settings, "max_flat_team_diff", 10);
        var maxWlDiff = GetIntSetting(settings, "max_wl_diff", 50);
        var maxKdDiff = GetIntSetting(settings, "max_kd_diff", 50);
        var maxSpecTypeDiff = GetIntSetting(settings, "max_spec_type_diff", 2);

        var combined = await LoadCombinedWeightsAsync(players, cancellationToken);
        if (combined.Missing.Count > 0)
        {
            return new ExperimentalBalanceServiceResult(
                false,
                null,
                new ExperimentalBalanceError(404, "One or more players are missing base weights or experimental spec weights.", combined.Missing));
        }

        var wlByPlayer = await LoadWlCurrentWeekAsync(players, cancellationToken);
        var specLogSets = await BuildSpecLogSetsAsync(cancellationToken);
        var latestSeason = await dbContext.TimeSeasons
            .AsNoTracking()
            .OrderByDescending(x => x.Id)
            .Select(x => new { x.Id, x.Timestamp })
            .FirstOrDefaultAsync(cancellationToken);
        dataFetchStopwatch.Stop();
        steps.Add(new ExperimentalBalanceMetaStep(
            Name: "db.query.playerData",
            DurationMs: dataFetchStopwatch.Elapsed.TotalMilliseconds,
            StartOffsetMs: dataFetchStartOffset));

        var random = Random.Shared;
        var computeStartOffset = requestStopwatch.Elapsed.TotalMilliseconds;
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var shuffledSpecs = GetLineupsNew(teamSize, random);
        var shuffledIndexMap = BuildShuffledIndexMap(shuffledSpecs);

        for (var iter = 0; iter < maxIter; iter++)
        {
            if (iter > 0 && iter % shuffleEvery == 0)
            {
                shuffledSpecs = GetLineupsNew(teamSize, random);
                shuffledIndexMap = BuildShuffledIndexMap(shuffledSpecs);
            }

            ShuffleInPlace(players, random);
            var blue = players.Take(teamSize).ToArray();
            var red = players.Skip(teamSize).Take(teamSize).ToArray();

            var blueAssign = AssignSpecs(blue, shuffledSpecs, shuffledIndexMap, specLogSets, combined.WeightsByPlayer);
            var redAssign = AssignSpecs(red, shuffledSpecs, shuffledIndexMap, specLogSets, combined.WeightsByPlayer);

            ApplySmallTeamDiscount(teamSize, blueAssign);
            ApplySmallTeamDiscount(teamSize, redAssign);

            var blueOff = blueAssign.Count(a => a.Off);
            var redOff = redAssign.Count(a => a.Off);

            var bw = blueAssign.Sum(a => a.EvalWeight);
            var rw = redAssign.Sum(a => a.EvalWeight);
            var weightDiff = Math.Abs(bw - rw);

            var flatDiff = Math.Abs(blueOff - redOff);

            var blueWl = SumWl(blueAssign, wlByPlayer);
            var redWl = SumWl(redAssign, wlByPlayer);
            var wlDiff = Math.Abs(blueWl - redWl);

            var blueKd = SumKd(blueAssign, wlByPlayer);
            var redKd = SumKd(redAssign, wlByPlayer);
            var kdDiff = Math.Abs(blueKd - redKd);

            var specTypeDiff = MaxSpecTypeDiff(blueAssign, redAssign);

            if (weightDiff <= maxWeightDiff
                && flatDiff <= maxFlatTeamDiff
                && wlDiff <= maxWlDiff
                && kdDiff <= maxKdDiff
                && specTypeDiff <= maxSpecTypeDiff)
            {
                sw.Stop();
                steps.Add(new ExperimentalBalanceMetaStep(
                    Name: "algorithm.computeBalance",
                    DurationMs: sw.Elapsed.TotalMilliseconds,
                    StartOffsetMs: computeStartOffset));

                var serializeStartOffset = requestStopwatch.Elapsed.TotalMilliseconds;
                var serializeStopwatch = System.Diagnostics.Stopwatch.StartNew();
                var namesByPlayer = await LoadPlayerNamesAsync(players, cancellationToken);
                var teamBalance = BuildTeamBalance(blueAssign, redAssign, wlByPlayer, namesByPlayer);
                serializeStopwatch.Stop();
                steps.Add(new ExperimentalBalanceMetaStep(
                    Name: "response.serialize",
                    DurationMs: serializeStopwatch.Elapsed.TotalMilliseconds,
                    StartOffsetMs: serializeStartOffset));

                var meta = new ExperimentalBalanceMeta(
                    Iterations: iter + 1,
                    DurationMs: requestStopwatch.Elapsed.TotalMilliseconds,
                    Steps: steps,
                    Season: latestSeason?.Id ?? 0,
                    Time: latestSeason?.Timestamp ?? DateTime.UtcNow);

                return new ExperimentalBalanceServiceResult(true, new ExperimentalBalanceResponse(teamBalance, meta), null);
            }
        }

        sw.Stop();
        return new ExperimentalBalanceServiceResult(
            false,
            null,
            new ExperimentalBalanceError(409, $"Could not find balanced teams after {maxIter} iterations."));
    }

    private static ExperimentalBalanceServiceResult Fail(int status, string message) =>
        new(false, null, new ExperimentalBalanceError(status, message));

    private static IReadOnlyList<ExperimentalBalanceTeam> BuildTeamBalance(
        PlayerAssignment[] blueAssign,
        PlayerAssignment[] redAssign,
        IReadOnlyDictionary<Guid, ExperimentalSpecsWlCurrentWeek> wlByPlayer,
        IReadOnlyDictionary<Guid, string> namesByPlayer)
    {
        return
        [
            BuildTeam(blueAssign, wlByPlayer, namesByPlayer),
            BuildTeam(redAssign, wlByPlayer, namesByPlayer)
        ];
    }

    private static ExperimentalBalanceTeam BuildTeam(
        IEnumerable<PlayerAssignment> assignments,
        IReadOnlyDictionary<Guid, ExperimentalSpecsWlCurrentWeek> wlByPlayer,
        IReadOnlyDictionary<Guid, string> namesByPlayer)
    {
        var specs = new Dictionary<string, ExperimentalBalancePlayerSpec>(StringComparer.Ordinal);
        var totalWeight = 0;
        var totalTalkers = 0;
        var totalWinLoss = 0;
        var totalKillDeath = 0;

        foreach (var assignment in assignments)
        {
            var (winLoss, killDeath) = GetSpecDiffs(assignment, wlByPlayer);
            var name = namesByPlayer.TryGetValue(assignment.PlayerId, out var playerName)
                ? playerName
                : string.Empty;
            var playerSpec = new ExperimentalBalancePlayerSpec(
                Name: name,
                Spec: assignment.Spec,
                Weight: assignment.EvalWeight,
                Talker: 0,
                WinLoss: winLoss,
                KillDeath: killDeath);

            specs[assignment.PlayerId.ToString()] = playerSpec;
            totalWeight += playerSpec.Weight;
            totalTalkers += playerSpec.Talker;
            totalWinLoss += playerSpec.WinLoss;
            totalKillDeath += playerSpec.KillDeath;
        }

        return new ExperimentalBalanceTeam(
            TotalWeight: totalWeight,
            TotalTalkers: totalTalkers,
            TotalWinLoss: totalWinLoss,
            TotalKillDeath: totalKillDeath,
            Specs: specs);
    }

    private static (int WinLoss, int KillDeath) GetSpecDiffs(
        PlayerAssignment assignment,
        IReadOnlyDictionary<Guid, ExperimentalSpecsWlCurrentWeek> wlByPlayer)
    {
        if (!wlByPlayer.TryGetValue(assignment.PlayerId, out var row))
        {
            return (0, 0);
        }

        var specIdx = Array.IndexOf(AllSpecsOrdered, assignment.Spec);
        if (specIdx < 0)
        {
            return (0, 0);
        }

        var (wins, losses, kills, deaths) = GetWlForSpec(row, specIdx);
        return (wins - losses, kills - deaths);
    }

    private static int GetIntSetting(IReadOnlyDictionary<string, decimal> settings, string key, int defaultValue) =>
        settings.TryGetValue(key, out var v) ? (int)Math.Round(v, MidpointRounding.AwayFromZero) : defaultValue;

    private async Task<(Dictionary<Guid, int[]> WeightsByPlayer, List<Guid> Missing)> LoadCombinedWeightsAsync(
        IReadOnlyList<Guid> players,
        CancellationToken cancellationToken)
    {
        var rows = await (
            from b in dbContext.BaseWeights.AsNoTracking()
            join s in dbContext.ExperimentalSpecWeights.AsNoTracking() on b.Uuid equals s.Uuid
            where players.Contains(b.Uuid)
            select new { b.Uuid, b.Weight, Spec = s }
        ).ToListAsync(cancellationToken);

        var dict = new Dictionary<Guid, int[]>();
        foreach (var row in rows)
        {
            dict[row.Uuid] = BuildCombinedWeightVector(row.Weight, row.Spec);
        }

        var missing = players.Where(p => !dict.ContainsKey(p)).ToList();
        return (dict, missing);
    }

    private async Task<Dictionary<Guid, string>> LoadPlayerNamesAsync(
        IReadOnlyList<Guid> players,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.Names
            .AsNoTracking()
            .Where(x => players.Contains(x.Uuid))
            .Select(x => new { x.Uuid, x.Name })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(x => x.Uuid)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.Name).FirstOrDefault() ?? string.Empty);
    }

    private static int[] BuildCombinedWeightVector(int baseWeight, ExperimentalSpecWeight spec) =>
    [
        baseWeight + spec.PyromancerOffset,
        baseWeight + spec.CryomancerOffset,
        baseWeight + spec.AquamancerOffset,
        baseWeight + spec.BerserkerOffset,
        baseWeight + spec.DefenderOffset,
        baseWeight + spec.RevenantOffset,
        baseWeight + spec.AvengerOffset,
        baseWeight + spec.CrusaderOffset,
        baseWeight + spec.ProtectorOffset,
        baseWeight + spec.ThunderlordOffset,
        baseWeight + spec.SpiritguardOffset,
        baseWeight + spec.EarthwardenOffset,
        baseWeight + spec.AssassinOffset,
        baseWeight + spec.VindicatorOffset,
        baseWeight + spec.ApothecaryOffset,
        baseWeight + spec.ConjurerOffset,
        baseWeight + spec.SentinelOffset,
        baseWeight + spec.LuminaryOffset
    ];

    private async Task<Dictionary<Guid, ExperimentalSpecsWlCurrentWeek>> LoadWlCurrentWeekAsync(
        IReadOnlyList<Guid> players,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.ExperimentalSpecsWlCurrentWeek
            .AsNoTracking()
            .Where(x => players.Contains(x.Uuid))
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(x => x.Uuid, x => x);
    }

    private async Task<Dictionary<string, HashSet<Guid>>> BuildSpecLogSetsAsync(CancellationToken cancellationToken)
    {
        var sets = AllSpecsOrdered.ToDictionary(s => s, _ => new HashSet<Guid>(), StringComparer.Ordinal);

        var logs = await dbContext.ExperimentalSpecLogs.AsNoTracking().ToListAsync(cancellationToken);
        foreach (var log in logs)
        {
            AddIfHasValue(sets, "Pyromancer", log.Pyromancer);
            AddIfHasValue(sets, "Cryomancer", log.Cryomancer);
            AddIfHasValue(sets, "Aquamancer", log.Aquamancer);
            AddIfHasValue(sets, "Berserker", log.Berserker);
            AddIfHasValue(sets, "Defender", log.Defender);
            AddIfHasValue(sets, "Revenant", log.Revenant);
            AddIfHasValue(sets, "Avenger", log.Avenger);
            AddIfHasValue(sets, "Crusader", log.Crusader);
            AddIfHasValue(sets, "Protector", log.Protector);
            AddIfHasValue(sets, "Thunderlord", log.Thunderlord);
            AddIfHasValue(sets, "Spiritguard", log.Spiritguard);
            AddIfHasValue(sets, "Earthwarden", log.Earthwarden);
            AddIfHasValue(sets, "Assassin", log.Assassin);
            AddIfHasValue(sets, "Vindicator", log.Vindicator);
            AddIfHasValue(sets, "Apothecary", log.Apothecary);
            AddIfHasValue(sets, "Conjurer", log.Conjurer);
            AddIfHasValue(sets, "Sentinel", log.Sentinel);
            AddIfHasValue(sets, "Luminary", log.Luminary);
        }

        return sets;
    }

    private static void AddIfHasValue(Dictionary<string, HashSet<Guid>> sets, string spec, Guid? uuid)
    {
        if (uuid.HasValue)
        {
            sets[spec].Add(uuid.Value);
        }
    }

    private static Dictionary<string, int> BuildShuffledIndexMap(IReadOnlyList<string> shuffledSpecs)
    {
        var map = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < shuffledSpecs.Count; i++)
        {
            map[shuffledSpecs[i]] = i;
        }

        return map;
    }

    /// <summary>Port of getLineupsNew from ExperimentalBalanceSpec.md (TS).</summary>
    internal static string[] GetLineupsNew(int playerCount, Random random)
    {
        var dmgSpecs = new[] { "Berserker", "Pyromancer", "Avenger", "Thunderlord", "Assassin", "Conjurer" };
        var tankSpecs = new[] { "Cryomancer", "Defender", "Vindicator", "Crusader", "Sentinel" };
        var pickSpecs = new[] { "Cryomancer", "Vindicator", "Crusader" };
        var healSpecs = new[] { "Aquamancer", "Revenant", "Protector", "Earthwarden", "Apothecary", "Luminary" };
        var mainHealer = random.NextDouble() < 0.5 ? "Luminary" : "Aquamancer";
        ShuffleInPlace(pickSpecs, random);

        var roleCounts = new Dictionary<int, (int Dmg, int Tank, int Heal, string[] Required)>
        {
            [6] = (2, 0, 1, ["Avenger", pickSpecs[0], mainHealer]),
            [7] = (2, 0, 1, ["Avenger", pickSpecs[0], pickSpecs[1], mainHealer]),
            [8] = (2, 2, 1, ["Avenger", "Defender", mainHealer]),
            [9] = (2, 2, 2, ["Avenger", "Defender", mainHealer]),
            [10] = (3, 2, 2, ["Avenger", "Defender", mainHealer]),
            [11] = (3, 3, 2, ["Avenger", "Defender", mainHealer]),
            [12] = (3, 3, 2, ["Avenger", "Defender", "Earthwarden", mainHealer]),
            [13] = (4, 3, 3, ["Avenger", "Defender", mainHealer]),
            [14] = (4, 4, 2, ["Avenger", "Defender", "Aquamancer", "Luminary"])
        };

        var (dmg, tank, heal, required) = roleCounts[playerCount];
        var lineup = new List<string>(required);
        var used = new HashSet<string>(required, StringComparer.Ordinal);

        string[] Pick(string[] pool, int count)
        {
            var available = pool.Where(s => !used.Contains(s)).ToList();
            ShuffleInPlace(available, random);
            if (playerCount is 7 or 8)
            {
                available = available.Where(s => s != "Assassin").ToList();
            }

            return available.Take(count).ToArray();
        }

        var pickedDmg = Pick(dmgSpecs, dmg);
        var pickedTank = Pick(tankSpecs, tank);
        var pickedHeal = Pick(healSpecs, heal);
        foreach (var s in pickedDmg)
        {
            used.Add(s);
        }

        foreach (var s in pickedTank)
        {
            used.Add(s);
        }

        foreach (var s in pickedHeal)
        {
            used.Add(s);
        }

        lineup.AddRange(pickedDmg);
        lineup.AddRange(pickedTank);
        lineup.AddRange(pickedHeal);

        if (lineup.Count > playerCount)
        {
            throw new InvalidOperationException($"Lineup exceeded player count: {lineup.Count} > {playerCount}");
        }

        if (lineup.Count < playerCount)
        {
            var allSpecs = dmgSpecs.Concat(tankSpecs).Concat(healSpecs).Where(s => !used.Contains(s)).ToList();
            ShuffleInPlace(allSpecs, random);
            lineup.AddRange(allSpecs.Take(playerCount - lineup.Count));
        }

        var arr = lineup.ToArray();
        ShuffleInPlace(arr, random);
        return arr;
    }

    private static void ShuffleInPlace<T>(IList<T> list, Random random)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private sealed class PlayerAssignment
    {
        public Guid PlayerId { get; init; }
        public string Spec { get; set; } = "Empty";
        public bool Off { get; set; }
        public int SpecWeight { get; set; }
        public int EvalWeight { get; set; }
    }

    private static PlayerAssignment[] AssignSpecs(
        Guid[] teamPlayerIds,
        IReadOnlyList<string> shuffledSpecs,
        IReadOnlyDictionary<string, int> shuffledSpecsIndexMap,
        IReadOnlyDictionary<string, HashSet<Guid>> specLogSets,
        IReadOnlyDictionary<Guid, int[]> weightsByPlayer)
    {
        var numberOfPlayers = teamPlayerIds.Length;
        var playersOnSpecs = new List<Guid>[numberOfPlayers];
        for (var i = 0; i < numberOfPlayers; i++)
        {
            playersOnSpecs[i] = [];
        }

        var assignments = new PlayerAssignment[numberOfPlayers];
        for (var i = 0; i < numberOfPlayers; i++)
        {
            var pid = teamPlayerIds[i];
            assignments[i] = new PlayerAssignment
            {
                PlayerId = pid,
                Spec = "Empty",
                Off = false,
                SpecWeight = 0,
                EvalWeight = 0
            };
        }

        var specStatus = new bool[numberOfPlayers];

        foreach (var spec in AllSpecsOrdered)
        {
            if (!shuffledSpecsIndexMap.TryGetValue(spec, out var idx) || idx >= numberOfPlayers)
            {
                continue;
            }

            foreach (var pid in teamPlayerIds)
            {
                playersOnSpecs[idx].Add(pid);
            }
        }

        for (var i = 0; i < playersOnSpecs.Length; i++)
        {
            var specName = shuffledSpecs[i];
            if (!specLogSets.TryGetValue(specName, out var logSet))
            {
                continue;
            }

            var list = playersOnSpecs[i];
            for (var j = list.Count - 1; j >= 0; j--)
            {
                if (logSet.Contains(list[j]))
                {
                    list.RemoveAt(j);
                }
            }
        }

        for (var round = 0; round < numberOfPlayers; round++)
        {
            var minIndex = 0;
            for (var k = 1; k < playersOnSpecs.Length; k++)
            {
                if (playersOnSpecs[k].Count < playersOnSpecs[minIndex].Count)
                {
                    minIndex = k;
                }
            }

            var pool = playersOnSpecs[minIndex];
            if (pool.Count != 0)
            {
                var random = Random.Shared;
                var randomPlayer = pool[random.Next(pool.Count)];
                for (var k = 0; k < numberOfPlayers; k++)
                {
                    if (assignments[k].PlayerId != randomPlayer)
                    {
                        continue;
                    }

                    assignments[k].Spec = shuffledSpecs[minIndex];
                    var w = weightsByPlayer[randomPlayer];
                    var specIdx = Array.IndexOf(AllSpecsOrdered, assignments[k].Spec);
                    var sw = specIdx >= 0 ? w[specIdx] : 0;
                    assignments[k].SpecWeight = sw;
                    assignments[k].EvalWeight = sw;

                    foreach (var specList in playersOnSpecs)
                    {
                        specList.Remove(randomPlayer);
                    }

                    for (var j = 0; j < 13; j++)
                    {
                        playersOnSpecs[minIndex].Add(Guid.Empty);
                    }

                    specStatus[minIndex] = true;
                    break;
                }
            }
            else
            {
                for (var j = 0; j < 13; j++)
                {
                    playersOnSpecs[minIndex].Add(Guid.Empty);
                }
            }
        }

        foreach (var a in assignments)
        {
            if (a.Spec != "Empty")
            {
                continue;
            }

            for (var i = 0; i < numberOfPlayers; i++)
            {
                if (specStatus[i])
                {
                    continue;
                }

                a.Spec = shuffledSpecs[i];
                a.Off = true;
                specStatus[i] = true;
                var w = weightsByPlayer[a.PlayerId];
                var specIdx = Array.IndexOf(AllSpecsOrdered, a.Spec);
                var sw = specIdx >= 0 ? w[specIdx] : 0;
                a.SpecWeight = sw;
                a.EvalWeight = sw;
                break;
            }
        }

        return assignments;
    }

    private static void ApplySmallTeamDiscount(int teamSize, PlayerAssignment[] team)
    {
        if (teamSize > 8)
        {
            return;
        }

        var total = team.Sum(t => t.EvalWeight);
        if (total == 0)
        {
            return;
        }

        foreach (var p in team)
        {
            var w = p.EvalWeight;
            if (w / (double)total > 0.3)
            {
                p.EvalWeight = (int)Math.Round(w * 0.8, MidpointRounding.AwayFromZero);
            }
        }
    }

    private static int SumWl(PlayerAssignment[] team, IReadOnlyDictionary<Guid, ExperimentalSpecsWlCurrentWeek> wlByPlayer)
    {
        var sum = 0;
        foreach (var a in team)
        {
            if (!wlByPlayer.TryGetValue(a.PlayerId, out var row))
            {
                continue;
            }

            var specIdx = Array.IndexOf(AllSpecsOrdered, a.Spec);
            if (specIdx < 0)
            {
                continue;
            }

            var (wins, losses, _, _) = GetWlForSpec(row, specIdx);
            sum += wins - losses;
        }

        return sum;
    }

    private static int SumKd(PlayerAssignment[] team, IReadOnlyDictionary<Guid, ExperimentalSpecsWlCurrentWeek> wlByPlayer)
    {
        var sum = 0;
        foreach (var a in team)
        {
            if (!wlByPlayer.TryGetValue(a.PlayerId, out var row))
            {
                continue;
            }

            var specIdx = Array.IndexOf(AllSpecsOrdered, a.Spec);
            if (specIdx < 0)
            {
                continue;
            }

            var (_, _, kills, deaths) = GetWlForSpec(row, specIdx);
            sum += kills - deaths;
        }

        return sum;
    }

    private static (int Wins, int Losses, int Kills, int Deaths) GetWlForSpec(ExperimentalSpecsWlCurrentWeek row, int specIdx) =>
        specIdx switch
        {
            0 => (row.PyromancerWins, row.PyromancerLosses, row.PyromancerKills, row.PyromancerDeaths),
            1 => (row.CryomancerWins, row.CryomancerLosses, row.CryomancerKills, row.CryomancerDeaths),
            2 => (row.AquamancerWins, row.AquamancerLosses, row.AquamancerKills, row.AquamancerDeaths),
            3 => (row.BerserkerWins, row.BerserkerLosses, row.BerserkerKills, row.BerserkerDeaths),
            4 => (row.DefenderWins, row.DefenderLosses, row.DefenderKills, row.DefenderDeaths),
            5 => (row.RevenantWins, row.RevenantLosses, row.RevenantKills, row.RevenantDeaths),
            6 => (row.AvengerWins, row.AvengerLosses, row.AvengerKills, row.AvengerDeaths),
            7 => (row.CrusaderWins, row.CrusaderLosses, row.CrusaderKills, row.CrusaderDeaths),
            8 => (row.ProtectorWins, row.ProtectorLosses, row.ProtectorKills, row.ProtectorDeaths),
            9 => (row.ThunderlordWins, row.ThunderlordLosses, row.ThunderlordKills, row.ThunderlordDeaths),
            10 => (row.SpiritguardWins, row.SpiritguardLosses, row.SpiritguardKills, row.SpiritguardDeaths),
            11 => (row.EarthwardenWins, row.EarthwardenLosses, row.EarthwardenKills, row.EarthwardenDeaths),
            12 => (row.AssassinWins, row.AssassinLosses, row.AssassinKills, row.AssassinDeaths),
            13 => (row.VindicatorWins, row.VindicatorLosses, row.VindicatorKills, row.VindicatorDeaths),
            14 => (row.ApothecaryWins, row.ApothecaryLosses, row.ApothecaryKills, row.ApothecaryDeaths),
            15 => (row.ConjurerWins, row.ConjurerLosses, row.ConjurerKills, row.ConjurerDeaths),
            16 => (row.SentinelWins, row.SentinelLosses, row.SentinelKills, row.SentinelDeaths),
            17 => (row.LuminaryWins, row.LuminaryLosses, row.LuminaryKills, row.LuminaryDeaths),
            _ => (0, 0, 0, 0)
        };

    private static int MaxSpecTypeDiff(PlayerAssignment[] blue, PlayerAssignment[] red)
    {
        static void CountRoles(PlayerAssignment[] team, out int dmg, out int tank, out int heal)
        {
            dmg = 0;
            tank = 0;
            heal = 0;
            foreach (var a in team)
            {
                if (DmgSpecs.Contains(a.Spec))
                {
                    dmg++;
                }
                else if (TankSpecs.Contains(a.Spec))
                {
                    tank++;
                }
                else if (HealSpecs.Contains(a.Spec))
                {
                    heal++;
                }
            }
        }

        CountRoles(blue, out var bd, out var bt, out var bh);
        CountRoles(red, out var rd, out var rt, out var rh);
        var d1 = Math.Abs(bd - rd);
        var d2 = Math.Abs(bt - rt);
        var d3 = Math.Abs(bh - rh);
        return Math.Max(d1, Math.Max(d2, d3));
    }
}
