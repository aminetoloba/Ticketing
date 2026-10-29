using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ticketing.Application.Common;
using Ticketing.Application.Common.Interfaces;
using Ticketing.Application.DTOs.CheckIn;
using Ticketing.Domain.Exceptions;

namespace Ticketing.Application.Services;

public class CheckInService : ICheckInService
{
    private readonly IEventRepository _events;
    private readonly ITicketRepository _tickets;

    public CheckInService(IEventRepository events, ITicketRepository tickets)
    {
        _events = events;
        _tickets = tickets;
    }

    public async Task<CheckInResultDto> CheckInAsync(
        Guid eventId, CheckInRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var @event = await _events.GetByIdAsync(eventId, cancellationToken)
            ?? throw new NotFoundException("Événement introuvable.");

        // 1. L'événement est-il ouvert au contrôle (statut et fenêtre horaire) ?
        @event.EnsureCheckInOpen(now);

        // 2. Le billet existe-t-il ? (les codes sont en majuscules : on tolère la saisie en minuscules)
        var code = request.Code.Trim().ToUpperInvariant();
        var ticket = await _tickets.GetByCodeAsync(code, cancellationToken)
            ?? throw new NotFoundException("Billet invalide : code inconnu.");

        // 3. Est-il bien pour cet événement ?
        if (ticket.EventId != eventId)
            throw new DomainException("Ce billet est pour un autre événement.");

        // 4. Valide -> Utilisé. Si le billet est déjà utilisé ou annulé, la méthode refuse.
        try
        {
            ticket.MarkAsUsed(now);
            await _tickets.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            // Un autre scan a passé le billet à « Utilisé » juste avant nous
            throw new TicketAlreadyUsedException("Ce billet vient d'être scanné par un autre contrôle.");
        }

        var typeName = @event.TicketTypes
            .FirstOrDefault(t => t.Id == ticket.TicketTypeId)?.Name ?? string.Empty;

        return new CheckInResultDto(ticket.Id, typeName, ticket.Status.ToString(), now);
    }
}
