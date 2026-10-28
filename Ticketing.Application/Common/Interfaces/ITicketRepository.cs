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
}
