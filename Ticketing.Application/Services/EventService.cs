using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ticketing.Application.Common;
using Ticketing.Application.Common.Interfaces;
using Ticketing.Application.DTOs.Events;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Enums;
using Ticketing.Domain.Exceptions;

namespace Ticketing.Application.Services;

public class EventService : IEventService
{
    private readonly IEventRepository _events;
    private readonly ICurrentUserService _currentUser;

    public EventService(IEventRepository events, ICurrentUserService currentUser)
    {
        _events = events;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<EventDto>> GetPublicAsync(
        EventQuery query, CancellationToken cancellationToken = default)
    {
        var (items, total) = await _events.SearchPublicAsync(query, cancellationToken);
        return ToPage(items, total, query);
    }

    public async Task<PagedResult<EventDto>> GetMineAsync(
        EventQuery query, CancellationToken cancellationToken = default)
    {
        var organizerId = RequireUserId();
        var (items, total) = await _events.SearchByOrganizerAsync(organizerId, query, cancellationToken);
        return ToPage(items, total, query);
    }

    public async Task<EventDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var @event = await GetEventAsync(id, cancellationToken);

        EnsureVisible(@event);

        return ToDto(@event);
    }

    public async Task<EventDto> CreateAsync(
        SaveEventRequest request, CancellationToken cancellationToken = default)
    {
        var organizerId = RequireUserId();
        var start = request.StartDate.UtcDateTime;
        var end = request.EndDate.UtcDateTime;

        if (start <= DateTime.UtcNow)
            throw new DomainException("La date de début doit être dans le futur.");

        var @event = Event.Create(
            request.Title,
            request.Description ?? string.Empty,
            request.Location,
            start,
            end,
            organizerId);

        await _events.AddAsync(@event, cancellationToken);
        await _events.SaveChangesAsync(cancellationToken);

        return ToDto(@event);
    }

    public async Task<EventDto> UpdateAsync(
        Guid id, SaveEventRequest request, CancellationToken cancellationToken = default)
    {
        var @event = await GetEventAsync(id, cancellationToken);

        @event.Update(
            request.Title,
            request.Description ?? string.Empty,
            request.Location,
            request.StartDate.UtcDateTime,
            request.EndDate.UtcDateTime);

        await _events.SaveChangesAsync(cancellationToken);
        return ToDto(@event);
    }

    public async Task<EventDto> PublishAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var @event = await GetEventAsync(id, cancellationToken);

        @event.Publish(DateTime.UtcNow);

        await _events.SaveChangesAsync(cancellationToken);
        return ToDto(@event);
    }

    public async Task<EventDto> PostponeAsync(
    Guid id, PostponeEventRequest request, CancellationToken cancellationToken = default)
    {
        var @event = await GetEventAsync(id, cancellationToken);

        @event.Postpone(
            request.NewStartDate.UtcDateTime,
            request.NewEndDate.UtcDateTime,
            request.Reason,
            DateTime.UtcNow);

        await _events.SaveChangesAsync(cancellationToken);
        return ToDto(@event);
    }

    public async Task<IReadOnlyList<TicketTypeDto>> GetTicketTypesAsync(
     Guid eventId, CancellationToken cancellationToken = default)
    {
        var @event = await GetEventAsync(eventId, cancellationToken);
        EnsureVisible(@event);

        var ticketTypes = await _events.GetTicketTypesAsync(eventId, cancellationToken);
        return ticketTypes.Select(ToTicketTypeDto).ToList();
    }

    public async Task<TicketTypeDto> AddTicketTypeAsync(
        Guid eventId, SaveTicketTypeRequest request, CancellationToken cancellationToken = default)
    {
        var @event = await GetEventAsync(eventId, cancellationToken);

        var ticketType = @event.AddTicketType(request.Name, request.Price, request.TotalQuantity);

        await _events.SaveChangesAsync(cancellationToken);
        return ToTicketTypeDto(ticketType);
    }
    private async Task<Event> GetEventAsync(Guid id, CancellationToken cancellationToken) =>
        await _events.GetByIdAsync(id, cancellationToken)
        ?? throw new NotFoundException("Événement introuvable.");

    private Guid RequireUserId() =>
        _currentUser.UserId ?? throw new AuthException("Utilisateur non authentifié.");

    private bool CanSeeDraft(Event @event) =>
        _currentUser.UserId == @event.OrganizerId || _currentUser.Role == Roles.Admin;

    private static PagedResult<EventDto> ToPage(
        IReadOnlyList<Event> items, int total, EventQuery query) =>
        new(items.Select(ToDto).ToList(), query.Page, query.PageSize, total);

    internal static EventDto ToDto(Event e) => new(
        e.Id,
        e.Title,
        e.Description,
        e.Location,
        e.StartDate,
        e.EndDate,
        e.Status.ToString(),
        e.OrganizerId,
        e.OriginalStartDate,
        e.OriginalEndDate,
        e.PostponementReason,
        e.PostponedAt,
        e.RefundWindowEndsAt
    );

    private void EnsureVisible(Event @event)
    {
        if (@event.Status == EventStatus.Draft && !CanSeeDraft(@event))
            throw new NotFoundException("Événement introuvable.");
    }

    private static TicketTypeDto ToTicketTypeDto(TicketType t) => new(
        t.Id,
        t.EventId,
        t.Name,
        t.Price,
        t.TotalQuantity,
        t.SoldQuantity,
        t.Remaining
    );
}
