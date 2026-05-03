using BalancerAPI.Common.Auth;
using Microsoft.AspNetCore.Authorization;

namespace BalancerAPI.Api.Security;

public static class AuthorizationPolicyExtensions
{
    public static void AddApiPermissionPolicies(this AuthorizationOptions options)
    {
        foreach (var p in ApiPermissions.All)
        {
            options.AddPolicy(p, policy => policy.RequireClaim("permission", p));
        }
    }
}
