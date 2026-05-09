using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Business.Services;

public interface INameUpdateService
{
    Task<IReadOnlyList<UpdatedNameEntry>> UpdateNamesAsync(CancellationToken cancellationToken);
}

public sealed class NameUpdateService(
    BalancerDbContext dbContext,
    IMinecraftPlayerResolveService minecraftPlayerResolveService) : INameUpdateService
{
    public async Task<IReadOnlyList<UpdatedNameEntry>> UpdateNamesAsync(CancellationToken cancellationToken)
    {
        var names = await dbContext.Names.ToListAsync(cancellationToken);
        var fetchTasks = names
            .Select(async row =>
            {
                var resolved = await minecraftPlayerResolveService.ResolveAsync(row.Uuid.ToString(), cancellationToken);
                return new FetchedNameResult(row, resolved);
            })
            .ToArray();

        var fetchedResults = await Task.WhenAll(fetchTasks);
        var updated = new List<UpdatedNameEntry>();

        foreach (var result in fetchedResults)
        {
            var row = result.Row;
            var resolved = result.Resolved;
            if (!resolved.Success)
            {
                continue;
            }

            var fetchedName = resolved.Name;

            if (string.IsNullOrWhiteSpace(fetchedName))
            {
                continue;
            }

            if (string.Equals(row.Name, fetchedName, StringComparison.Ordinal))
            {
                continue;
            }

            var previous = row.Name;
            row.PreviousNames ??= [];
            if (!string.IsNullOrWhiteSpace(previous) &&
                !row.PreviousNames.Contains(previous, StringComparer.OrdinalIgnoreCase))
            {
                row.PreviousNames = [.. row.PreviousNames, previous];
            }

            row.Name = fetchedName;
            updated.Add(new UpdatedNameEntry(row.Uuid, previous, fetchedName));
        }

        if (updated.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return updated;
    }
}

public sealed record UpdatedNameEntry(Guid Uuid, string Previous, string Current);

public sealed record UpdateNamesResponse(IReadOnlyList<UpdatedNameEntry> Updated);

internal sealed record FetchedNameResult(PlayerName Row, PlayerResolveResult Resolved);