using BalancerAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace BalancerAPI.Controllers;

[ApiController]
[Route("experimental/spec-weights")]
public class ExperimentalSpecWeightsController(ISpecWeightsService specWeightsService) : ControllerBase
{
    [HttpGet("{uuid:guid}")]
    public async Task<ActionResult<SpecWeightsResponse>> Get(Guid uuid, CancellationToken cancellationToken)
    {
        var result = await specWeightsService.GetCombinedAsync(uuid, cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }
}
