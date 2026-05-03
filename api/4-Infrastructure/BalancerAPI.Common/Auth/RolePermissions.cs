namespace BalancerAPI.Common.Auth;

public static class RolePermissions
{
    private static readonly IReadOnlyDictionary<string, HashSet<string>> Map =
        new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
        {
            [ApiRoles.BotFull] =
            [
                ApiPermissions.NamesUpdate,
                ApiPermissions.TimeRead,
                ApiPermissions.TimeWrite,
                ApiPermissions.SettingsRead,
                ApiPermissions.SettingsWrite,
                ApiPermissions.ExperimentalRead,
                ApiPermissions.ExperimentalBalance,
                ApiPermissions.ExperimentalConfirm,
                ApiPermissions.ExperimentalInput,
                ApiPermissions.AdjustAuto,
                ApiPermissions.AdjustManual
            ],
            [ApiRoles.WebReadOnly] =
            [
                ApiPermissions.TimeRead,
                ApiPermissions.SettingsRead,
                ApiPermissions.ExperimentalRead
            ]
        };

    public static bool TryResolvePermissions(IEnumerable<string> roles, out HashSet<string> permissions)
    {
        permissions = new HashSet<string>(StringComparer.Ordinal);
        foreach (var role in roles)
        {
            // Reject null/empty entries (Postgres text[] permits NULL elements; bad data must fail closed).
            if (string.IsNullOrEmpty(role) || !Map.TryGetValue(role, out var set))
            {
                permissions.Clear();
                return false;
            }

            foreach (var p in set)
            {
                permissions.Add(p);
            }
        }

        return permissions.Count > 0;
    }
}
