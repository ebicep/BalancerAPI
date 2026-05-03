using BalancerAPI.Common.Auth;

namespace BalancerAPI.Tests.Auth;

public sealed class RolePermissionsTests
{
    [Fact]
    public void TryResolvePermissions_BotFull_CoversAllPermissions()
    {
        Assert.True(RolePermissions.TryResolvePermissions([ApiRoles.BotFull], out var perms));
        foreach (var p in ApiPermissions.All)
        {
            Assert.Contains(p, perms);
        }
    }

    [Fact]
    public void TryResolvePermissions_WebReadOnly_HasReadEndpointsOnly()
    {
        Assert.True(RolePermissions.TryResolvePermissions([ApiRoles.WebReadOnly], out var perms));
        Assert.Contains(ApiPermissions.SettingsRead, perms);
        Assert.Contains(ApiPermissions.TimeRead, perms);
        Assert.Contains(ApiPermissions.ExperimentalRead, perms);
        Assert.DoesNotContain(ApiPermissions.SettingsWrite, perms);
    }

    [Fact]
    public void TryResolvePermissions_UnknownRole_ReturnsFalse()
    {
        Assert.False(RolePermissions.TryResolvePermissions(["not-a-role"], out _));
    }

    [Fact]
    public void TryResolvePermissions_EmptyRoles_ReturnsFalse()
    {
        Assert.False(RolePermissions.TryResolvePermissions([], out _));
    }

    [Fact]
    public void TryResolvePermissions_NullElement_ReturnsFalse()
    {
        Assert.False(RolePermissions.TryResolvePermissions([ApiRoles.BotFull, null!], out _));
    }

    [Fact]
    public void TryResolvePermissions_EmptyStringElement_ReturnsFalse()
    {
        Assert.False(RolePermissions.TryResolvePermissions([ApiRoles.BotFull, ""], out _));
    }
}
