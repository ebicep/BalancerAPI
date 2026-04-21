using Asp.Versioning;
using BalancerAPI.Business.Services;
using Microsoft.AspNetCore.Mvc;

namespace BalancerAPI.Api.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class AdjustController(
    IAdjustmentAutoDailyService adjustmentAutoDailyService) : ControllerBase
{
    [HttpPost("auto-daily")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(AdjustmentAutoDailyResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdjustmentAutoDailyResponse>> AutoDaily(CancellationToken cancellationToken)
    {
        var result = await adjustmentAutoDailyService.ApplyAutoDailyAsync(cancellationToken);
        return Ok(result);
    }
}
