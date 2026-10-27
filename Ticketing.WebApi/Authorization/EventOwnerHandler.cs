using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Ticketing.Application.Common;
using Ticketing.Application.Common.Interfaces;

namespace Ticketing.WebApi.Authorization;

public class EventOwnerHandler : AuthorizationHandler<EventOwnerRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IEventRepository _events;

    public EventOwnerHandler(
        IHttpContextAccessor httpContextAccessor, IEventRepository events)
    {
        _httpContextAccessor = httpContextAccessor;
        _events = events;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, EventOwnerRequirement requirement)
    {
        var user = context.User;

        // Un admin peut agir sur tous les événements
        if (user.IsInRole(Roles.Admin))
        {
            context.Succeed(requirement);
            return;
        }

        // Les autres rôles (Participant, Staff) n'ont aucun droit de propriétaire
        if (!user.IsInRole(Roles.Organizer))
            return;

        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return;

        // L'identifiant de l'événement vient de l'URL : /events/{id}/...
        var httpContext = _httpContextAccessor.HttpContext;
        if (!Guid.TryParse(httpContext?.GetRouteValue("id")?.ToString(), out var eventId))
            return;

        var organizerId = await _events.GetOrganizerIdAsync(
            eventId, httpContext!.RequestAborted);

        // Événement inexistant : on laisse passer pour que le service réponde 404
        // plutôt qu'un 403 trompeur
        if (organizerId is null || organizerId == userId)
            context.Succeed(requirement);
    }
}