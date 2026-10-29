using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticketing.Application.Common.Interfaces;
using Ticketing.Application.DTOs.CheckIn;
using Ticketing.WebApi.Authorization;

namespace Ticketing.WebApi.Controllers;

// Le paramètre doit s'appeler « id » : la policy le lit sous ce nom
[ApiController]
[Route("events/{id:guid}/check-in")]
[Authorize(Policy = Policies.IsAssignedStaff)]
public class CheckInController : ControllerBase
{
    private readonly ICheckInService _checkIn;
    private readonly IValidator<CheckInRequest> _validator;

    public CheckInController(ICheckInService checkIn, IValidator<CheckInRequest> validator)
    {
        _checkIn = checkIn;
        _validator = validator;
    }

    [HttpPost]
    public async Task<ActionResult<CheckInResultDto>> CheckIn(
        Guid id, CheckInRequest request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await _checkIn.CheckInAsync(id, request, cancellationToken));
    }
}