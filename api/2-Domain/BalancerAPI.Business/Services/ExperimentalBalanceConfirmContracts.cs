namespace BalancerAPI.Business.Services;

public interface IExperimentalBalanceConfirmService
{
    Task<ExperimentalBalanceConfirmServiceResult> ConfirmAsync(Guid balanceId, CancellationToken cancellationToken);
    Task<ExperimentalBalanceConfirmServiceResult> UnconfirmAsync(Guid balanceId, CancellationToken cancellationToken);
}

public sealed record ExperimentalBalanceConfirmServiceResult(
    bool Success,
    int StatusCode,
    string? Message);
