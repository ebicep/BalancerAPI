using Asp.Versioning;
using BalancerAPI.Business.Services;
using Microsoft.AspNetCore.Mvc;

namespace BalancerAPI.Api.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class AdjustController(
    IAdjustmentAutoDailyService adjustmentAutoDailyService,
    IAdjustmentAutoWeeklyService adjustmentAutoWeeklyService) : ControllerBase
{
    [HttpPost("auto-daily")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(AdjustmentAutoDailyResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdjustmentAutoDailyResponse>> AutoDaily(CancellationToken cancellationToken)
    {
        var result = await adjustmentAutoDailyService.ApplyAutoDailyAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("auto-weekly")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(AdjustmentAutoWeeklyResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdjustmentAutoWeeklyResponse>> AutoWeekly(CancellationToken cancellationToken)
    {
        var result = await adjustmentAutoWeeklyService.ApplyAutoWeeklyAsync(cancellationToken);
        return Ok(result);
    }
}
