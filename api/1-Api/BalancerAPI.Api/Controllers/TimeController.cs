using Asp.Versioning;
using BalancerAPI.Common.Auth;
using BalancerAPI.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BalancerAPI.Api.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class TimeController(ITimeService timeService) : ControllerBase
{
    [HttpPost("new-day")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.TimeWrite)]
    [ProducesResponseType(typeof(NewDayResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<NewDayResponse>> NewDay(CancellationToken cancellationToken)
    {
        var newDay = await timeService.CreateNewDayAsync(cancellationToken);
        return Ok(new NewDayResponse(newDay));
    }

    [HttpDelete("day/{dayId:int}")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.TimeWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UndoDay(int dayId, CancellationToken cancellationToken)
    {
        var wasUndone = await timeService.UndoDayAsync(dayId, cancellationToken);
        return wasUndone ? NoContent() : NotFound();
    }

    [HttpPost("new-week")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.TimeWrite)]
    [ProducesResponseType(typeof(NewWeekResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<NewWeekResponse>> NewWeek(CancellationToken cancellationToken)
    {
        var newWeek = await timeService.CreateNewWeekAsync(cancellationToken);
        return Ok(newWeek);
    }

    [HttpDelete("week/{weekId:int}")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.TimeWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UndoWeek(int weekId, CancellationToken cancellationToken)
    {
        var wasUndone = await timeService.UndoWeekAsync(weekId, cancellationToken);
        return wasUndone ? NoContent() : NotFound();
    }

    [HttpPost("new-season")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.TimeWrite)]
    [ProducesResponseType(typeof(NewSeasonResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<NewSeasonResponse>> NewSeason(CancellationToken cancellationToken)
    {
        var (season, timestamp) = await timeService.CreateNewSeasonAsync(cancellationToken);
        return Ok(new NewSeasonResponse(season, timestamp));
    }

    [HttpDelete("season/{seasonId:int}")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.TimeWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UndoSeason(int seasonId, CancellationToken cancellationToken)
    {
        var wasUndone = await timeService.UndoSeasonAsync(seasonId, cancellationToken);
        return wasUndone ? NoContent() : NotFound();
    }

    [HttpGet("season")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.TimeRead)]
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