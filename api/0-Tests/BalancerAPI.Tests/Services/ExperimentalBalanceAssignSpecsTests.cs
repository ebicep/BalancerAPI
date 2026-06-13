using BalancerAPI.Business.Services;

namespace BalancerAPI.Tests.Services;

public class ExperimentalBalanceAssignSpecsTests
{
    private static Dictionary<string, HashSet<Guid>> EmptyLogSets() =>
        ExperimentalSpecs.AllOrdered.ToDictionary(s => s, _ => new HashSet<Guid>(), StringComparer.Ordinal);

    private static Dictionary<string, int> ShuffledMap(IReadOnlyList<string> specs)
    {
        var map = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < specs.Count; i++)
        {
            map[specs[i]] = i;
        }

        return map;
    }

    private static Guid P(int i) => Guid.Parse($"00000000-0000-0000-0000-{i:000000000012}");

    private static Dictionary<string, int> AllOrderedIndexMap() =>
        ExperimentalSpecs.AllOrdered
            .Select((name, idx) => (name, idx))
            .ToDictionary(x => x.name, x => x.idx, StringComparer.Ordinal);

    [Fact]
    public void AssignSpecs_never_assigns_banned_spec_across_iterations()
    {
        var random = new Random(911);
        var shuffledSpecs = ExperimentalBalanceService.GetLineupsNew(6, random);
        var bannedSpec = shuffledSpecs[0];
        var banIdx = Array.IndexOf(ExperimentalSpecs.AllOrdered, bannedSpec);
        Assert.True(banIdx >= 0);

        var bannedPlayer = P(1);
        var team = new[] { bannedPlayer, P(2), P(3), P(4), P(5), P(6) };
        var weights = team.ToDictionary(
            id => id,
            _ => Enumerable.Repeat(100, ExperimentalSpecs.AllOrdered.Length).ToArray());

        var bans = new Dictionary<Guid, bool[]>
        {
            [bannedPlayer] = new bool[ExperimentalSpecs.AllOrdered.Length]
        };
        bans[bannedPlayer][banIdx] = true;

        var logSets = EmptyLogSets();
        var specNameToIdx = AllOrderedIndexMap();
        var shuffledIndexMap = ShuffledMap(shuffledSpecs);

        for (var iter = 0; iter < 500; iter++)
        {
            var result = ExperimentalBalanceService.AssignSpecs(
                team,
                shuffledSpecs,
                shuffledIndexMap,
                logSets,
                bans,
                specNameToIdx,
                weights);

            Assert.False(ExperimentalBalanceService.HasIncompleteSpecAssignment(result));
            foreach (var a in result)
            {
                if (a.PlayerId == bannedPlayer)
                {
                    Assert.NotEqual(bannedSpec, a.Spec);
                }
            }
        }
    }

    [Fact]
    public void AssignSpecs_when_player_banned_from_entire_lineup_yields_incomplete_assignment()
    {
        var random = new Random(42);
        var shuffledSpecs = ExperimentalBalanceService.GetLineupsNew(6, random);
        var victim = P(1);
        var team = new[] { victim, P(2), P(3), P(4), P(5), P(6) };
        var weights = team.ToDictionary(
            id => id,
            _ => Enumerable.Repeat(100, ExperimentalSpecs.AllOrdered.Length).ToArray());

        var vec = new bool[ExperimentalSpecs.AllOrdered.Length];
        foreach (var spec in shuffledSpecs)
        {
            var idx = Array.IndexOf(ExperimentalSpecs.AllOrdered, spec);
            if (idx >= 0)
            {
                vec[idx] = true;
            }
        }

        var bans = new Dictionary<Guid, bool[]> { [victim] = vec };
        var result = ExperimentalBalanceService.AssignSpecs(
            team,
            shuffledSpecs,
            ShuffledMap(shuffledSpecs),
            EmptyLogSets(),
            bans,
            AllOrderedIndexMap(),
            weights);

        Assert.True(ExperimentalBalanceService.HasIncompleteSpecAssignment(result));
    }

    [Fact]
    public void AssignSpecs_player_in_all_spec_logs_gets_off_true()
    {
        var random = new Random(123);
        var shuffledSpecs = ExperimentalBalanceService.GetLineupsNew(6, random);
        var repeater = P(1);
        var team = new[] { repeater, P(2), P(3), P(4), P(5), P(6) };
        var weights = team.ToDictionary(
            id => id,
            _ => Enumerable.Repeat(100, ExperimentalSpecs.AllOrdered.Length).ToArray());

        var logSets = EmptyLogSets();
        foreach (var spec in ExperimentalSpecs.AllOrdered)
        {
            logSets[spec].Add(repeater);
        }

        var noBans = new Dictionary<Guid, bool[]>();

        var result = ExperimentalBalanceService.AssignSpecs(
            team,
            shuffledSpecs,
            ShuffledMap(shuffledSpecs),
            logSets,
            noBans,
            AllOrderedIndexMap(),
            weights);

        Assert.False(ExperimentalBalanceService.HasIncompleteSpecAssignment(result));

        var repeaterAssignment = result.Single(a => a.PlayerId == repeater);
        Assert.True(repeaterAssignment.Off,
            $"Player in all spec logs should be Off=true but got Off=false, assigned spec: {repeaterAssignment.Spec}");
        Assert.NotEqual(ExperimentalSpecs.Empty, repeaterAssignment.Spec);
    }

    [Fact]
    public void AssignSpecs_preassigns_requested_spec_when_eligible()
    {
        var random = new Random(7);
        var shuffledSpecs = ExperimentalBalanceService.GetLineupsNew(6, random);
        var requestedSpec = shuffledSpecs[0];
        var requester = P(1);
        var team = new[] { requester, P(2), P(3), P(4), P(5), P(6) };
        var weights = team.ToDictionary(
            id => id,
            _ => Enumerable.Repeat(100, ExperimentalSpecs.AllOrdered.Length).ToArray());

        var preassigns = new List<ExperimentalBalanceService.SpecRequestPreassign>
        {
            new(requester, requestedSpec)
        };

        var result = ExperimentalBalanceService.AssignSpecs(
            team,
            shuffledSpecs,
            ShuffledMap(shuffledSpecs),
            EmptyLogSets(),
            new Dictionary<Guid, bool[]>(),
            AllOrderedIndexMap(),
            weights,
            preassigns);

        var assignment = result.Single(a => a.PlayerId == requester);
        Assert.Equal(requestedSpec, assignment.Spec);
        Assert.False(assignment.Off);
    }

    [Fact]
    public void AssignSpecs_preassign_skipped_when_spec_in_log()
    {
        var random = new Random(7);
        var shuffledSpecs = ExperimentalBalanceService.GetLineupsNew(6, random);
        var requestedSpec = shuffledSpecs[0];
        var requester = P(1);
        var team = new[] { requester, P(2), P(3), P(4), P(5), P(6) };
        var weights = team.ToDictionary(
            id => id,
            _ => Enumerable.Repeat(100, ExperimentalSpecs.AllOrdered.Length).ToArray());

        var logSets = EmptyLogSets();
        logSets[requestedSpec].Add(requester);

        var preassigns = new List<ExperimentalBalanceService.SpecRequestPreassign>
        {
            new(requester, requestedSpec)
        };

        var result = ExperimentalBalanceService.AssignSpecs(
            team,
            shuffledSpecs,
            ShuffledMap(shuffledSpecs),
            logSets,
            new Dictionary<Guid, bool[]>(),
            AllOrderedIndexMap(),
            weights,
            preassigns);

        var assignment = result.Single(a => a.PlayerId == requester);
        Assert.NotEqual(requestedSpec, assignment.Spec);
    }

    [Fact]
    public void AssignSpecs_when_multiple_players_request_same_spec_only_one_gets_that_spec()
    {
        var random = new Random(7);
        var shuffledSpecs = ExperimentalBalanceService.GetLineupsNew(6, random);
        var requestedSpec = shuffledSpecs[0];
        var requester1 = P(1);
        var requester2 = P(2);
        var requester3 = P(3);
        var team = new[] { requester1, requester2, requester3, P(4), P(5), P(6) };
        var weights = team.ToDictionary(
            id => id,
            _ => Enumerable.Repeat(100, ExperimentalSpecs.AllOrdered.Length).ToArray());

        var preassigns = new List<ExperimentalBalanceService.SpecRequestPreassign>
        {
            new(requester1, requestedSpec),
            new(requester2, requestedSpec),
            new(requester3, requestedSpec)
        };

        var result = ExperimentalBalanceService.AssignSpecs(
            team,
            shuffledSpecs,
            ShuffledMap(shuffledSpecs),
            EmptyLogSets(),
            new Dictionary<Guid, bool[]>(),
            AllOrderedIndexMap(),
            weights,
            preassigns);

        Assert.False(ExperimentalBalanceService.HasIncompleteSpecAssignment(result));

        var specs = result.Select(a => a.Spec).ToList();
        Assert.Equal(specs.Count, specs.Distinct(StringComparer.Ordinal).Count());

        var requestersWithSpec = result
            .Where(a => a.Spec == requestedSpec)
            .Select(a => a.PlayerId)
            .ToList();
        Assert.Single(requestersWithSpec);
        Assert.Equal(requester1, requestersWithSpec[0]);
    }

    [Fact]
    public void AssignSpecs_different_spec_requester_honored_after_same_spec_slot_taken_skips()
    {
        var random = new Random(7);
        var shuffledSpecs = ExperimentalBalanceService.GetLineupsNew(6, random);
        var contestedSpec = shuffledSpecs[0];
        var differentSpec = shuffledSpecs[1];
        var sameSpecRequesters = new[] { P(1), P(2), P(3), P(4), P(5) };
        var differentSpecRequester = P(6);
        var team = new[] { sameSpecRequesters[0], sameSpecRequesters[1], sameSpecRequesters[2],
            sameSpecRequesters[3], sameSpecRequesters[4], differentSpecRequester };
        var weights = team.ToDictionary(
            id => id,
            _ => Enumerable.Repeat(100, ExperimentalSpecs.AllOrdered.Length).ToArray());

        var preassigns = new List<ExperimentalBalanceService.SpecRequestPreassign>();
        foreach (var requester in sameSpecRequesters)
        {
            preassigns.Add(new ExperimentalBalanceService.SpecRequestPreassign(requester, contestedSpec));
        }

        preassigns.Add(new ExperimentalBalanceService.SpecRequestPreassign(differentSpecRequester, differentSpec));

        var result = ExperimentalBalanceService.AssignSpecs(
            team,
            shuffledSpecs,
            ShuffledMap(shuffledSpecs),
            EmptyLogSets(),
            new Dictionary<Guid, bool[]>(),
            AllOrderedIndexMap(),
            weights,
            preassigns,
            requestSpecsLimit: 4);

        Assert.False(ExperimentalBalanceService.HasIncompleteSpecAssignment(result));

        var differentSpecAssignment = result.Single(a => a.PlayerId == differentSpecRequester);
        Assert.Equal(differentSpec, differentSpecAssignment.Spec);
        Assert.False(differentSpecAssignment.Off);
    }

    [Fact]
    public void ShuffleInPlaceRespectingPins_keeps_pinned_players_at_same_index()
    {
        var p1 = P(1);
        var p2 = P(2);
        var p3 = P(3);
        var p4 = P(4);
        var players = new List<Guid> { p1, p2, p3, p4 };
        var pinned = new HashSet<Guid> { p2 };

        for (var i = 0; i < 200; i++)
        {
            var copy = new List<Guid>(players);
            ExperimentalBalanceService.ShuffleInPlaceRespectingPins(copy, pinned, new Random(i));
            Assert.Equal(p2, copy[1]);
        }
    }
}
