using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ticketing.Application.DTOs.Tickets;
using Ticketing.Domain.Entities;

namespace Ticketing.Application.Common.Interfaces;

public interface ITicketRepository
{
    Task<IReadOnlyList<TicketDto>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    // Null si le billet n'existe pas OU n'appartient pas à cet utilisateur
    Task<Ticket?> GetOwnedAsync(Guid ticketId, Guid userId, CancellationToken cancellationToken = default);

    // Billet suivi par EF, pour pouvoir le modifier
    Task<Ticket?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    // Lève ConcurrencyConflictException si le billet a changé entre-temps
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
