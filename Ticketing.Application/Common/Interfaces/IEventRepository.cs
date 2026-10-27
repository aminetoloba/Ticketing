using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ticketing.Application.DTOs.Events;
using Ticketing.Domain.Entities;

namespace Ticketing.Application.Common.Interfaces;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid?> GetOrganizerIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Event> Items, int TotalCount)> SearchPublicAsync(
        EventQuery query, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Event> Items, int TotalCount)> SearchByOrganizerAsync(
        Guid organizerId, EventQuery query, CancellationToken cancellationToken = default);

    Task AddAsync(Event @event, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
