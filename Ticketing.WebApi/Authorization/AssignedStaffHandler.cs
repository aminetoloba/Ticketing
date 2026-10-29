using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Ticketing.Application.Common;
using Ticketing.Application.Common.Interfaces;

namespace Ticketing.WebApi.Authorization;

public class AssignedStaffHandler : AuthorizationHandler<AssignedStaffRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IStaffRepository _staff;

    public AssignedStaffHandler(IHttpContextAccessor httpContextAccessor, IStaffRepository staff)
    {
        _httpContextAccessor = httpContextAccessor;
        _staff = staff;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, AssignedStaffRequirement requirement)
    {
        var user = context.User;

        // Un admin peut contrôler n'importe quel événement
        if (user.IsInRole(Roles.Admin))
        {
            context.Succeed(requirement);
            return;
        }

        // Organisateurs et participants ne scannent pas (matrice de ton dossier)
        if (!user.IsInRole(Roles.Staff))
            return;

        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return;

        var httpContext = _httpContextAccessor.HttpContext;
        if (!Guid.TryParse(httpContext?.GetRouteValue("id")?.ToString(), out var eventId))
            return;

        // Pas d'affectation active : refusé. Même pour un événement inexistant,
        // pour ne rien révéler au staff.
        if (await _staff.IsAssignedAsync(userId, eventId, httpContext!.RequestAborted))
            context.Succeed(requirement);
    }
}