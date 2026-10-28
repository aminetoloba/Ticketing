using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ticketing.Application.DTOs.Tickets;

public record TicketDto(
    Guid Id,
    Guid OrderId,
    Guid EventId,
    string EventTitle,
    string EventLocation,
    DateTime EventStartDate,
    DateTime EventEndDate,
    string EventStatus,
    string TicketTypeName,
    string Status,
    string Code
);
