using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticketing.Application.Common.Interfaces;
using Ticketing.Application.DTOs.Events;
using Ticketing.WebApi.Authorization;

namespace Ticketing.WebApi.Controllers;

[ApiController]
[Route("events")]
public class EventsController : ControllerBase
{
    private readonly IEventService _events;
    private readonly IValidator<SaveEventRequest> _saveValidator;
    private readonly IValidator<EventQuery> _queryValidator;
    private readonly IValidator<SaveTicketTypeRequest> _ticketTypeValidator;

    public EventsController(
        IEventService events,
        IValidator<SaveEventRequest> saveValidator,
        IValidator<EventQuery> queryValidator,
        IValidator<SaveTicketTypeRequest> ticketTypeValidator)
    {
        _events = events;
        _saveValidator = saveValidator;
        _queryValidator = queryValidator;
        _ticketTypeValidator = ticketTypeValidator;
    }

    // Catalogue public : pas besoin d'être connecté
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResult<EventDto>>> GetPublic(
        [FromQuery] EventQuery query, CancellationToken cancellationToken)
    {
        await _queryValidator.ValidateAndThrowAsync(query, cancellationToken);
        return Ok(await _events.GetPublicAsync(query, cancellationToken));
    }

    // Mes événements, brouillons compris
    [HttpGet("mine")]
    [Authorize(Roles = Policies.OrganizerOrAdmin)]
    public async Task<ActionResult<PagedResult<EventDto>>> GetMine(
        [FromQuery] EventQuery query, CancellationToken cancellationToken)
    {
        await _queryValidator.ValidateAndThrowAsync(query, cancellationToken);
        return Ok(await _events.GetMineAsync(query, cancellationToken));
    }

    // Public, mais un brouillon reste invisible sauf pour son propriétaire :
    // le token est lu s'il est envoyé, même sur une route anonyme
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<EventDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await _events.GetByIdAsync(id, cancellationToken));

    // Niveau 1 : le rôle suffit
    [HttpPost]
    [Authorize(Roles = Policies.OrganizerOrAdmin)]
    public async Task<ActionResult<EventDto>> Create(
        SaveEventRequest request, CancellationToken cancellationToken)
    {
        await _saveValidator.ValidateAndThrowAsync(request, cancellationToken);
        var created = await _events.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // Niveau 2 : il faut être le propriétaire de cet événement (ou admin)
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.IsEventOwner)]
    public async Task<ActionResult<EventDto>> Update(
        Guid id, SaveEventRequest request, CancellationToken cancellationToken)
    {
        await _saveValidator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await _events.UpdateAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/publish")]
    [Authorize(Policy = Policies.IsEventOwner)]
    public async Task<ActionResult<EventDto>> Publish(Guid id, CancellationToken cancellationToken) =>
        Ok(await _events.PublishAsync(id, cancellationToken));

    [HttpGet("{id:guid}/ticket-types")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<TicketTypeDto>>> GetTicketTypes(
    Guid id, CancellationToken cancellationToken) =>
    Ok(await _events.GetTicketTypesAsync(id, cancellationToken));

    [HttpPost("{id:guid}/ticket-types")]
    [Authorize(Policy = Policies.IsEventOwner)]
    public async Task<ActionResult<TicketTypeDto>> AddTicketType(
        Guid id, SaveTicketTypeRequest request, CancellationToken cancellationToken)
    {
        await _ticketTypeValidator.ValidateAndThrowAsync(request, cancellationToken);
        var created = await _events.AddTicketTypeAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(GetTicketTypes), new { id }, created);
    }
}