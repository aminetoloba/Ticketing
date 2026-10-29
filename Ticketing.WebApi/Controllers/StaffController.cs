using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticketing.Application.Common.Interfaces;
using Ticketing.Application.DTOs.Staff;
using Ticketing.WebApi.Authorization;

namespace Ticketing.WebApi.Controllers;

// Le paramètre doit s'appeler « id » : la policy IsEventOwner le lit sous ce nom
[ApiController]
[Route("events/{id:guid}/staff")]
[Authorize(Policy = Policies.IsEventOwner)]
public class StaffController : ControllerBase
{
    private readonly IStaffService _staff;
    private readonly IValidator<AssignStaffRequest> _assignValidator;

    public StaffController(IStaffService staff, IValidator<AssignStaffRequest> assignValidator)
    {
        _staff = staff;
        _assignValidator = assignValidator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StaffMemberDto>>> GetStaff(
        Guid id, CancellationToken cancellationToken) =>
        Ok(await _staff.GetAsync(id, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<StaffMemberDto>> Assign(
        Guid id, AssignStaffRequest request, CancellationToken cancellationToken)
    {
        await _assignValidator.ValidateAndThrowAsync(request, cancellationToken);
        var created = await _staff.AssignAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(GetStaff), new { id }, created);
    }

    [HttpDelete("{staffId:guid}")]
    public async Task<IActionResult> Remove(
        Guid id, Guid staffId, CancellationToken cancellationToken)
    {
        await _staff.RemoveAsync(id, staffId, cancellationToken);
        return NoContent();
    }
}