using Asp.Versioning;
using BalancerAPI.Business.Services;
using BalancerAPI.Common.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BalancerAPI.Api.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class PatchController(IWeeklyAdjustPatchService weeklyAdjustPatchService) : ControllerBase
{
    [HttpPatch("weekly-adjusts")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.AdjustAuto)]
    [ProducesResponseType(typeof(WeeklyAdjustPatchResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<WeeklyAdjustPatchResponse>> WeeklyAdjusts(CancellationToken cancellationToken)
    {
        var result = await weeklyAdjustPatchService.ApplyPatchAsync(cancellationToken);
        return Ok(result);
    }
}
