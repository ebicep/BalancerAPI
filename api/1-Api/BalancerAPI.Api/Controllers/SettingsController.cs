using Asp.Versioning;
using BalancerAPI.Common.Auth;
using BalancerAPI.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BalancerAPI.Api.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class SettingsController(ISettingsService settingsService) : ControllerBase
{
    [HttpGet]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.SettingsRead)]
    [ProducesResponseType(typeof(SettingsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SettingsResponse>> GetAll(CancellationToken cancellationToken)
    {
        var data = await settingsService.GetAllAsync(cancellationToken);
        return Ok(new SettingsResponse(data));
    }

    [HttpGet("{key}")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.SettingsRead)]
    [ProducesResponseType(typeof(SettingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SettingResponse>> GetByKey(string key, CancellationToken cancellationToken)
    {
        var setting = await settingsService.GetByKeyAsync(key, cancellationToken);
        if (setting is null)
        {
            return NotFound();
        }

        return Ok(new SettingResponse(new SettingResponseData(setting.Key, setting.Value, setting.DisplayName)));
    }

    [HttpPost("{key}")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.SettingsWrite)]
    [ProducesResponseType(typeof(SettingResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SettingResponse>> Upsert(
        string key,
        [FromBody] UpdateSettingRequest request,
        CancellationToken cancellationToken)
    {
        var setting = await settingsService.UpsertAsync(key, request.Value, cancellationToken);
        return Ok(new SettingResponse(new SettingResponseData(setting.Key, setting.Value, setting.DisplayName)));
    }
}
