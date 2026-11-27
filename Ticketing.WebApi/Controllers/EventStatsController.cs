using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticketing.Application.Common.Interfaces;
using Ticketing.Application.DTOs.Stats;
using Ticketing.WebApi.Authorization;

namespace Ticketing.WebApi.Controllers;

// Le paramètre doit s'appeler « id » : la policy IsEventOwner le lit sous ce nom
[ApiController]
[Route("events/{id:guid}/stats")]
[Authorize(Policy = Policies.IsEventOwner)]
public class EventStatsController : ControllerBase
{
    private readonly IStatsService _stats;

    public EventStatsController(IStatsService stats)
    {
        _stats = stats;
    }

    [HttpGet]
    public async Task<ActionResult<EventStatsDto>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await _stats.GetAsync(id, cancellationToken));
}