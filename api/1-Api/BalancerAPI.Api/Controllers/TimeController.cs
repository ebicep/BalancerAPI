using Asp.Versioning;
using BalancerAPI.Business.Services;
using Microsoft.AspNetCore.Mvc;

namespace BalancerAPI.Api.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("time")]
public class TimeController(ITimeService timeService) : ControllerBase
{
    [HttpPost("new-day")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(NewDayResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<NewDayResponse>> NewDay(CancellationToken cancellationToken)
    {
        var newDay = await timeService.CreateNewDayAsync(cancellationToken);
        return Ok(new NewDayResponse(newDay));
    }

    [HttpPost("new-week")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(NewWeekResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<NewWeekResponse>> NewWeek(CancellationToken cancellationToken)
    {
        var newWeek = await timeService.CreateNewWeekAsync(cancellationToken);
        return Ok(new NewWeekResponse(newWeek));
    }
}