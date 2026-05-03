namespace BalancerAPI.Common.Auth;

/// <summary>Permission strings; policy names match these values.</summary>
public static class ApiPermissions
{
    public const string NamesUpdate = "names.update";

    public const string TimeRead = "time.read";
    public const string TimeWrite = "time.write";

    public const string SettingsRead = "settings.read";
    public const string SettingsWrite = "settings.write";

    public const string ExperimentalRead = "experimental.read";
    public const string ExperimentalBalance = "experimental.balance";
    public const string ExperimentalConfirm = "experimental.confirm";
    public const string ExperimentalInput = "experimental.input";

    public const string AdjustAuto = "adjust.auto";
    public const string AdjustManual = "adjust.manual";

    public static readonly IReadOnlyList<string> All =
    [
        NamesUpdate,
        TimeRead,
        TimeWrite,
        SettingsRead,
        SettingsWrite,
        ExperimentalRead,
        ExperimentalBalance,
        ExperimentalConfirm,
        ExperimentalInput,
        AdjustAuto,
        AdjustManual
    ];
}
