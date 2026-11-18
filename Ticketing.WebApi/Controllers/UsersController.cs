using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticketing.Application.Common;
using Ticketing.Application.Common.Interfaces;
using Ticketing.Application.DTOs.Events;
using Ticketing.Application.DTOs.Users;

namespace Ticketing.WebApi.Controllers;

[ApiController]
[Route("users")]
[Authorize(Roles = Roles.Admin)]
public class UsersController : ControllerBase
{
    private readonly IUserAdminService _users;
    private readonly IValidator<UserQuery> _queryValidator;
    private readonly IValidator<ChangeRoleRequest> _roleValidator;

    public UsersController(
        IUserAdminService users,
        IValidator<UserQuery> queryValidator,
        IValidator<ChangeRoleRequest> roleValidator)
    {
        _users = users;
        _queryValidator = queryValidator;
        _roleValidator = roleValidator;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<UserDto>>> GetUsers(
        [FromQuery] UserQuery query, CancellationToken cancellationToken)
    {
        await _queryValidator.ValidateAndThrowAsync(query, cancellationToken);
        return Ok(await _users.GetUsersAsync(query, cancellationToken));
    }

    [HttpPut("{id:guid}/role")]
    public async Task<ActionResult<UserDto>> ChangeRole(
        Guid id, ChangeRoleRequest request, CancellationToken cancellationToken)
    {
        await _roleValidator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await _users.ChangeRoleAsync(id, request, cancellationToken));
    }
}