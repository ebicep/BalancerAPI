using BalancerAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace BalancerAPI.Controllers;

[ApiController]
[Route("names")]
public class NamesController(INameUpdateService nameUpdateService) : ControllerBase
{
    [HttpPost("update")]
    public async Task<ActionResult<UpdateNamesResponse>> Update(CancellationToken cancellationToken)
    {
        var updated = await nameUpdateService.UpdateNamesAsync(cancellationToken);
        return Ok(new UpdateNamesResponse(updated));
    }
}
