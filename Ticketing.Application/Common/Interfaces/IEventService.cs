using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ticketing.Application.DTOs.Events;

namespace Ticketing.Application.Common.Interfaces;

public interface IEventService
{
    Task<PagedResult<EventDto>> GetPublicAsync(EventQuery query, CancellationToken cancellationToken = default);
    Task<PagedResult<EventDto>> GetMineAsync(EventQuery query, CancellationToken cancellationToken = default);
    Task<EventDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EventDto> CreateAsync(SaveEventRequest request, CancellationToken cancellationToken = default);
    Task<EventDto> UpdateAsync(Guid id, SaveEventRequest request, CancellationToken cancellationToken = default);
    Task<EventDto> PublishAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EventDto> PostponeAsync(Guid id, PostponeEventRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TicketTypeDto>> GetTicketTypesAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<TicketTypeDto> AddTicketTypeAsync(Guid eventId, SaveTicketTypeRequest request, CancellationToken cancellationToken = default);
}
