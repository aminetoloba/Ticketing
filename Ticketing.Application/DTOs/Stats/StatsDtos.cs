using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ticketing.Application.DTOs.Stats;

public record TicketTypeStatsDto(
    Guid TicketTypeId,
    string Name,
    decimal Price,
    int Capacity,
    int Sold,
    int Reserved,
    decimal Revenue,
    decimal FillRate);

public record DailySalesDto(DateOnly Date, int Orders, int Tickets, decimal Revenue);

public record EventStatsDto(
    Guid EventId,
    int Capacity,
    int TicketsSold,
    int TicketsReserved,
    int TicketsRemaining,
    decimal FillRate,
    decimal Revenue,
    decimal RefundedAmount,
    int CheckedIn,
    decimal CheckInRate,
    IReadOnlyList<TicketTypeStatsDto> ByTicketType,
    IReadOnlyList<DailySalesDto> SalesByDay);
