using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Business.Services;

public interface IExperimentalSpecBanService
{
    Task<ExperimentalSpecBanServiceResult> GetBansAsync(Guid uuid, CancellationToken cancellationToken);

    Task<ExperimentalSpecBanServiceResult> SetBanAsync(
        Guid uuid,
        string canonicalSpec,
        bool banned,
        CancellationToken cancellationToken);
}

public sealed class ExperimentalSpecBanService(IDbContextFactory<BalancerDbContext> dbContextFactory)
    : IExperimentalSpecBanService
{
    public async Task<ExperimentalSpecBanServiceResult> GetBansAsync(Guid uuid, CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var row = await db.ExperimentalSpecBans
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Uuid == uuid, cancellationToken);

        return Ok(BansFromRow(row));
    }

    public async Task<ExperimentalSpecBanServiceResult> SetBanAsync(
        Guid uuid,
        string canonicalSpec,
        bool banned,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var row = await db.ExperimentalSpecBans
            .FirstOrDefaultAsync(x => x.Uuid == uuid, cancellationToken);

        if (!banned)
        {
            if (row is null || !GetBanFlag(row, canonicalSpec))
            {
                return Fail(400, $"Player is not banned from {canonicalSpec}.");
            }
        }
        else if (row is not null && GetBanFlag(row, canonicalSpec))
        {
            return Fail(400, $"Player is already banned from {canonicalSpec}.");
        }

        if (row is null)
        {
            row = new ExperimentalSpecBan { Uuid = uuid };
            db.ExperimentalSpecBans.Add(row);
        }

        SetBanFlag(row, canonicalSpec, banned);
        await db.SaveChangesAsync(cancellationToken);

        return Ok(BansFromRow(row));
    }

    private static ExperimentalSpecBanServiceResult Ok(IReadOnlyList<string> bans) =>
        new(true, 200, null, new ExperimentalSpecBansResponse(bans));

    private static ExperimentalSpecBanServiceResult Fail(int statusCode, string message) =>
        new(false, statusCode, message, null);

    private static IReadOnlyList<string> BansFromRow(ExperimentalSpecBan? row)
    {
        if (row is null)
        {
            return [];
        }

        var bans = new List<string>(ExperimentalSpecs.AllOrdered.Length);
        foreach (var spec in ExperimentalSpecs.AllOrdered)
        {
            if (GetBanFlag(row, spec))
            {
                bans.Add(spec);
            }
        }

        return bans;
    }

    private static bool GetBanFlag(ExperimentalSpecBan row, string spec) =>
        spec switch
        {
            "Pyromancer" => row.Pyromancer,
            "Cryomancer" => row.Cryomancer,
            "Aquamancer" => row.Aquamancer,
            "Berserker" => row.Berserker,
            "Defender" => row.Defender,
            "Revenant" => row.Revenant,
            "Avenger" => row.Avenger,
            "Crusader" => row.Crusader,
            "Protector" => row.Protector,
            "Thunderlord" => row.Thunderlord,
            "Spiritguard" => row.Spiritguard,
            "Earthwarden" => row.Earthwarden,
            "Assassin" => row.Assassin,
            "Vindicator" => row.Vindicator,
            "Apothecary" => row.Apothecary,
            "Conjurer" => row.Conjurer,
            "Sentinel" => row.Sentinel,
            "Luminary" => row.Luminary,
            _ => false
        };

    private static void SetBanFlag(ExperimentalSpecBan row, string spec, bool banned)
    {
        switch (spec)
        {
            case "Pyromancer": row.Pyromancer = banned; break;
            case "Cryomancer": row.Cryomancer = banned; break;
            case "Aquamancer": row.Aquamancer = banned; break;
            case "Berserker": row.Berserker = banned; break;
            case "Defender": row.Defender = banned; break;
            case "Revenant": row.Revenant = banned; break;
            case "Avenger": row.Avenger = banned; break;
            case "Crusader": row.Crusader = banned; break;
            case "Protector": row.Protector = banned; break;
            case "Thunderlord": row.Thunderlord = banned; break;
            case "Spiritguard": row.Spiritguard = banned; break;
            case "Earthwarden": row.Earthwarden = banned; break;
            case "Assassin": row.Assassin = banned; break;
            case "Vindicator": row.Vindicator = banned; break;
            case "Apothecary": row.Apothecary = banned; break;
            case "Conjurer": row.Conjurer = banned; break;
            case "Sentinel": row.Sentinel = banned; break;
            case "Luminary": row.Luminary = banned; break;
        }
    }
}
