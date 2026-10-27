using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ticketing.Application.DTOs.Events;

public record SaveTicketTypeRequest(string Name, decimal Price, int TotalQuantity);

public record TicketTypeDto(
    Guid Id,
    Guid EventId,
    string Name,
    decimal Price,
    int TotalQuantity,
    int SoldQuantity,
    int Remaining
);
