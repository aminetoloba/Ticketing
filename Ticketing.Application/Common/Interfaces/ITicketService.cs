using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ticketing.Application.DTOs.Tickets;

namespace Ticketing.Application.Common.Interfaces;

public interface ITicketService
{
    Task<IReadOnlyList<TicketDto>> GetMineAsync(CancellationToken cancellationToken = default);
    Task<byte[]> GetQrAsync(Guid ticketId, CancellationToken cancellationToken = default);
}
