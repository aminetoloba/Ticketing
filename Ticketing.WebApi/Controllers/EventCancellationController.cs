using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticketing.Application.Common.Interfaces;
using Ticketing.Application.DTOs.Events;
using Ticketing.WebApi.Authorization;

namespace Ticketing.WebApi.Controllers;

// Le paramètre doit s'appeler « id » : la policy IsEventOwner le lit sous ce nom
[ApiController]
[Route("events/{id:guid}/cancel")]
[Authorize(Policy = Policies.IsEventOwner)]
public class EventCancellationController : ControllerBase
{
    private readonly IEventCancellationService _cancellation;

    public EventCancellationController(IEventCancellationService cancellation)
    {
        _cancellation = cancellation;
    }

    [HttpPost]
    public async Task<ActionResult<CancelEventResultDto>> Cancel(
        Guid id, CancellationToken cancellationToken) =>
        Ok(await _cancellation.CancelAsync(id, cancellationToken));
}