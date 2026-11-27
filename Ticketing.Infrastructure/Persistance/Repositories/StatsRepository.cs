using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ticketing.Application.Common.Interfaces;
using Ticketing.Domain.Enums;

namespace Ticketing.Infrastructure.Persistance.Repositories;

public class StatsRepository : IStatsRepository
{
    private readonly AppDbContext _db;

    public StatsRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TicketTypeInfo>> GetTicketTypesAsync(
        Guid eventId, CancellationToken cancellationToken = default) =>
        await _db.TicketTypes
            .AsNoTracking()
            .Where(t => t.EventId == eventId)
            .OrderBy(t => t.Price).ThenBy(t => t.Name)
            .Select(t => new TicketTypeInfo(t.Id, t.Name, t.Price, t.TotalQuantity))
            .ToListAsync(cancellationToken);

    // Billets vendus par type : le GroupBy est traduit en SQL, la base ne renvoie qu'une ligne par type
    public async Task<IReadOnlyList<TicketCountRow>> GetPaidTicketCountsAsync(
        Guid eventId, CancellationToken cancellationToken = default) =>
        await _db.Tickets
            .AsNoTracking()
            .Where(t => t.EventId == eventId
                        && (t.Status == TicketStatus.Valid || t.Status == TicketStatus.Used))
            .GroupBy(t => t.TicketTypeId)
            .Select(g => new TicketCountRow(
                g.Key,
                g.Count(),
                g.Count(t => t.Status == TicketStatus.Used)))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TicketTypeAmountRow>> GetPaidRevenueByTypeAsync(
        Guid eventId, CancellationToken cancellationToken = default) =>
        await (from i in _db.OrderItems.AsNoTracking()
               join o in _db.Orders on i.OrderId equals o.Id
               where o.EventId == eventId && o.Status == OrderStatus.Paid
               group i by i.TicketTypeId into g
               select new TicketTypeAmountRow(
                   g.Key,
                   g.Sum(x => x.Quantity),
                   g.Sum(x => x.Quantity * x.UnitPrice)))
            .ToListAsync(cancellationToken);

    // Réservations en attente de paiement dont le délai n'est pas échu
    public async Task<IReadOnlyList<TicketTypeAmountRow>> GetPendingByTypeAsync(
        Guid eventId, DateTime utcNow, CancellationToken cancellationToken = default) =>
        await (from i in _db.OrderItems.AsNoTracking()
               join o in _db.Orders on i.OrderId equals o.Id
               where o.EventId == eventId
                     && o.Status == OrderStatus.Pending
                     && o.ExpiresAt > utcNow
               group i by i.TicketTypeId into g
               select new TicketTypeAmountRow(g.Key, g.Sum(x => x.Quantity), 0m))
            .ToListAsync(cancellationToken);

    // Ventes par jour : la date est celle du paiement réussi (en UTC)
    public async Task<IReadOnlyList<DailyRow>> GetDailySalesAsync(
        Guid eventId, CancellationToken cancellationToken = default) =>
        await (from p in _db.Payments.AsNoTracking()
               join o in _db.Orders on p.OrderId equals o.Id
               join i in _db.OrderItems on o.Id equals i.OrderId
               where o.EventId == eventId
                     && o.Status == OrderStatus.Paid
                     && p.Status == PaymentStatus.Succeeded
               group new { o, i } by p.CreatedAt.Date into g
               orderby g.Key
               select new DailyRow(
                   g.Key,
                   g.Select(x => x.o.Id).Distinct().Count(),
                   g.Sum(x => x.i.Quantity),
                   g.Sum(x => x.i.Quantity * x.i.UnitPrice)))
            .ToListAsync(cancellationToken);

    public async Task<decimal> GetRefundedAmountAsync(
        Guid eventId, CancellationToken cancellationToken = default) =>
        await (from p in _db.Payments.AsNoTracking()
               join o in _db.Orders on p.OrderId equals o.Id
               where o.EventId == eventId && p.Status == PaymentStatus.Refunded
               select p.Amount)
            .SumAsync(cancellationToken);
}