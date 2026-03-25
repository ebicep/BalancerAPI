using BalancerAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace BalancerAPI.Controllers;

[ApiController]
[Route("time")]
public class TimeController(ITimeService timeService) : ControllerBase
{
    [HttpPost("new-day")]
    public async Task<ActionResult<NewDayResponse>> NewDay(CancellationToken cancellationToken)
    {
        var newDay = await timeService.CreateNewDayAsync(cancellationToken);
        return Ok(new NewDayResponse(newDay));
    }

    [HttpPost("new-week")]
    public async Task<ActionResult<NewWeekResponse>> NewWeek(CancellationToken cancellationToken)
    {
        var newWeek = await timeService.CreateNewWeekAsync(cancellationToken);
        return Ok(new NewWeekResponse(newWeek));
    }
}
