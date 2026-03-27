using Asp.Versioning;
using BalancerAPI.Business.Services;
using Microsoft.AspNetCore.Mvc;

namespace BalancerAPI.Api.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class ExperimentalController(
    ISpecWeightsService specWeightsService,
    IExperimentalBalanceService experimentalBalanceService) : ControllerBase
{
    [HttpGet("spec-weights/{uuid:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(SpecWeightsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SpecWeightsResponse>> GetSpecWeights(Guid uuid, CancellationToken cancellationToken)
    {
        var result = await specWeightsService.GetCombinedAsync(uuid, cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpPost("balance")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(ExperimentalBalanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExperimentalBalanceResponse>> Balance(
        [FromBody] ExperimentalBalanceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await experimentalBalanceService.BalanceAsync(request, cancellationToken);
        if (result.Success && result.Data is not null)
        {
            return Ok(result.Data);
        }

        var err = result.Error!;
        return err.StatusCode switch
        {
            400 => BadRequest(err.Message),
            404 => NotFound(err),
            409 => Conflict(err.Message),
            _ => StatusCode(err.StatusCode, err.Message)
        };
    }
}
