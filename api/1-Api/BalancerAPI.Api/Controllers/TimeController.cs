using Asp.Versioning;
using BalancerAPI.Business.Services;
using Microsoft.AspNetCore.Mvc;

namespace BalancerAPI.Api.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
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

    [HttpPost("new-season")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(NewSeasonResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<NewSeasonResponse>> NewSeason(CancellationToken cancellationToken)
    {
        var (season, timestamp) = await timeService.CreateNewSeasonAsync(cancellationToken);
        return Ok(new NewSeasonResponse(season, timestamp));
    }

    [HttpGet("season")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(LatestSeasonResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LatestSeasonResponse>> GetSeason(CancellationToken cancellationToken)
    {
        var latest = await timeService.GetLatestSeasonAsync(cancellationToken);
        if (latest is null)
        {
            return NotFound();
        }

        var (season, timestamp) = latest.Value;
        return Ok(new LatestSeasonResponse(season, timestamp));
    }
}