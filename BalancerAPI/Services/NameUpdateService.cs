using System.Net.Http.Json;
using BalancerAPI.Data;
using BalancerAPI.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Services;

public interface INameUpdateService
{
    Task<IReadOnlyList<UpdatedNameEntry>> UpdateNamesAsync(CancellationToken cancellationToken);
}

public sealed class NameUpdateService(BalancerDbContext dbContext, HttpClient httpClient) : INameUpdateService
{
    public async Task<IReadOnlyList<UpdatedNameEntry>> UpdateNamesAsync(CancellationToken cancellationToken)
    {
        var names = await dbContext.Names.ToListAsync(cancellationToken);
        var fetchTasks = names
            .Select(async row =>
            {
                var uuidNoDash = row.Uuid.ToString("N").ToLowerInvariant();
                var profile = await FetchProfileAsync(uuidNoDash, cancellationToken);
                return new FetchedNameResult(row, profile?.Name);
            })
            .ToArray();

        var fetchedResults = await Task.WhenAll(fetchTasks);
        var updated = new List<UpdatedNameEntry>();

        foreach (var result in fetchedResults)
        {
            var row = result.Row;
            var fetchedName = result.FetchedName;

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

    private async Task<MojangProfileResponse?> FetchProfileAsync(string uuidNoDash, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync($"session/minecraft/profile/{uuidNoDash}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<MojangProfileResponse>(cancellationToken);
    }
}

public sealed record UpdatedNameEntry(Guid Uuid, string Previous, string Current);

public sealed record UpdateNamesResponse(IReadOnlyList<UpdatedNameEntry> Updated);

internal sealed record MojangProfileResponse(
    string? Id,
    string? Name,
    IReadOnlyList<MojangProfileProperty>? Properties,
    IReadOnlyList<string>? ProfileActions);

internal sealed record MojangProfileProperty(string? Name, string? Value);

internal sealed record FetchedNameResult(PlayerName Row, string? FetchedName);
