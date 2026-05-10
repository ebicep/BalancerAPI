using System.Text.Json;

namespace BalancerAPI.Business.Services;

/// <summary>Builds a template <see cref="ExperimentalBalanceInputBody"/> from stored <c>experimental_balance_log.balance</c> JSON.</summary>
public static class ExperimentalBalanceMockInputBodyBuilder
{
    /// <summary>Valid MongoDB ObjectId hex placeholder; replace with a real game id before POST …/input.</summary>
    public const string PlaceholderGameId = "000000000000000000000000";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public sealed record BuildResult(bool Success, ExperimentalBalanceInputBody? Body, string? Error);

    /// <summary>Team index 0 → winners, index 1 → losers; each line gets random kills/deaths in [0, 15].</summary>
    public static BuildResult TryBuild(string balanceJson)
    {
        List<ExperimentalBalanceTeam>? teams;
        try
        {
            teams = JsonSerializer.Deserialize<List<ExperimentalBalanceTeam>>(balanceJson, JsonOptions);
        }
        catch (JsonException)
        {
            return new BuildResult(false, null, "Stored balance JSON is invalid.");
        }

        if (teams is null || teams.Count != 2)
        {
            return new BuildResult(false, null, "Stored balance must contain exactly two teams.");
        }

        var winners = teams[0].Specs.Select(s => new ExperimentalBalanceInputPlayerLine(s.Uuid, RandomKd(), RandomKd())).ToList();
        var losers = teams[1].Specs.Select(s => new ExperimentalBalanceInputPlayerLine(s.Uuid, RandomKd(), RandomKd())).ToList();
        return new BuildResult(true, new ExperimentalBalanceInputBody(winners, losers, PlaceholderGameId), null);
    }

    private static int RandomKd() => Random.Shared.Next(0, 16);
}
