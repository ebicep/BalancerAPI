using Asp.Versioning;
using BalancerAPI.Common.Auth;
using BalancerAPI.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BalancerAPI.Api.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class NamesController(INameUpdateService nameUpdateService) : ControllerBase
{
    [HttpPost("update")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.NamesUpdate)]
    [ProducesResponseType(typeof(UpdateNamesResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UpdateNamesResponse>> Update(CancellationToken cancellationToken)
    {
        var updated = await nameUpdateService.UpdateNamesAsync(cancellationToken);
        return Ok(new UpdateNamesResponse(updated));
    }
}