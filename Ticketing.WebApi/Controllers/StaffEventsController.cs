using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticketing.Application.Common;
using Ticketing.Application.Common.Interfaces;
using Ticketing.Application.DTOs.Events;

namespace Ticketing.WebApi.Controllers;

[ApiController]
[Route("staff/events")]
[Authorize(Roles = Roles.Staff)]
public class StaffEventsController : ControllerBase
{
    private readonly IStaffService _staff;

    public StaffEventsController(IStaffService staff)
    {
        _staff = staff;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EventDto>>> GetMine(
        CancellationToken cancellationToken) =>
        Ok(await _staff.GetMyEventsAsync(cancellationToken));
}