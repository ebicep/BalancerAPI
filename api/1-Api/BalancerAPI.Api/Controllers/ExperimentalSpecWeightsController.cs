using Asp.Versioning;
using BalancerAPI.Business.Services;
using Microsoft.AspNetCore.Mvc;

namespace BalancerAPI.Api.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("experimental/spec-weights")]
public class ExperimentalSpecWeightsController(ISpecWeightsService specWeightsService) : ControllerBase
{
    [HttpGet("{uuid:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(SpecWeightsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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