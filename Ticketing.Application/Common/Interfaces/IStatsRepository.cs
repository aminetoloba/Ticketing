using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ticketing.Application.Common.Interfaces;

public record TicketTypeInfo(Guid Id, string Name, decimal Price, int Capacity);
public record TicketCountRow(Guid TicketTypeId, int Sold, int Used);
public record TicketTypeAmountRow(Guid TicketTypeId, int Quantity, decimal Amount);
public record DailyRow(DateTime Day, int Orders, int Tickets, decimal Revenue);

public interface IStatsRepository
{
    Task<IReadOnlyList<TicketTypeInfo>> GetTicketTypesAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TicketCountRow>> GetPaidTicketCountsAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TicketTypeAmountRow>> GetPaidRevenueByTypeAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TicketTypeAmountRow>> GetPendingByTypeAsync(Guid eventId, DateTime utcNow, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DailyRow>> GetDailySalesAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<decimal> GetRefundedAmountAsync(Guid eventId, CancellationToken cancellationToken = default);
}
