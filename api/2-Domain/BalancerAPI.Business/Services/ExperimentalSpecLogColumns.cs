using BalancerAPI.Domain.Models;

namespace BalancerAPI.Business.Services;

internal static class ExperimentalSpecLogColumns
{
    public static IEnumerable<(string Spec, Guid Uuid)> EnumerateAssignments(ExperimentalSpecLog row)
    {
        if (row.Pyromancer is { } pyromancer) yield return ("Pyromancer", pyromancer);
        if (row.Cryomancer is { } cryomancer) yield return ("Cryomancer", cryomancer);
        if (row.Aquamancer is { } aquamancer) yield return ("Aquamancer", aquamancer);
        if (row.Berserker is { } berserker) yield return ("Berserker", berserker);
        if (row.Defender is { } defender) yield return ("Defender", defender);
        if (row.Revenant is { } revenant) yield return ("Revenant", revenant);
        if (row.Avenger is { } avenger) yield return ("Avenger", avenger);
        if (row.Crusader is { } crusader) yield return ("Crusader", crusader);
        if (row.Protector is { } protector) yield return ("Protector", protector);
        if (row.Thunderlord is { } thunderlord) yield return ("Thunderlord", thunderlord);
        if (row.Spiritguard is { } spiritguard) yield return ("Spiritguard", spiritguard);
        if (row.Earthwarden is { } earthwarden) yield return ("Earthwarden", earthwarden);
        if (row.Assassin is { } assassin) yield return ("Assassin", assassin);
        if (row.Vindicator is { } vindicator) yield return ("Vindicator", vindicator);
        if (row.Apothecary is { } apothecary) yield return ("Apothecary", apothecary);
        if (row.Conjurer is { } conjurer) yield return ("Conjurer", conjurer);
        if (row.Sentinel is { } sentinel) yield return ("Sentinel", sentinel);
        if (row.Luminary is { } luminary) yield return ("Luminary", luminary);
    }
}
