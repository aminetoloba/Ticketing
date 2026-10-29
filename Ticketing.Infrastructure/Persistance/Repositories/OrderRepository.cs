using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ticketing.Application.Common;
using Ticketing.Application.Common.Interfaces;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Enums;
using Ticketing.Domain.Enums;

namespace Ticketing.Infrastructure.Persistance.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _db;

    public OrderRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default) =>
        await _db.Orders.AddAsync(order, cancellationToken);

    public async Task<IReadOnlyList<Order>> GetByUserAsync(
        Guid userId, CancellationToken cancellationToken = default) =>
        await _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Order>> GetExpiredPendingAsync(
        DateTime utcNow, int take, CancellationToken cancellationToken = default) =>
        await _db.Orders
            .Include(o => o.Items)
            .Where(o => o.Status == OrderStatus.Pending && o.ExpiresAt <= utcNow)
            .OrderBy(o => o.ExpiresAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TicketType>> GetTicketTypesForUpdateAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) =>
        await _db.TicketTypes
            .Where(t => ids.Contains(t.Id))
            .ToListAsync(cancellationToken);

    public Task<Order?> GetByIdForPaymentAsync(
    Guid orderId, CancellationToken cancellationToken = default) =>
    _db.Orders
        .Include(o => o.Items)
        .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

    public async Task AddPaymentAsync(Payment payment, CancellationToken cancellationToken = default) =>
        await _db.Payments.AddAsync(payment, cancellationToken);

    public Task<Payment?> GetPaymentAsync(
        Guid paymentId, CancellationToken cancellationToken = default) =>
        _db.Payments.FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

    public async Task<IReadOnlyList<Order>> GetPendingByEventAsync(
    Guid eventId, CancellationToken cancellationToken = default) =>
    await _db.Orders
        .Where(o => o.EventId == eventId && o.Status == OrderStatus.Pending)
        .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetPaidOrderIdsByEventAsync(
        Guid eventId, CancellationToken cancellationToken = default) =>
        await _db.Orders
            .Where(o => o.EventId == eventId && o.Status == OrderStatus.Paid)
            .OrderBy(o => o.CreatedAt)
            .Select(o => o.Id)
            .ToListAsync(cancellationToken);

    // Avec ses billets : Order.Cancel() les annule aussi
    public Task<Order?> GetForRefundAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        _db.Orders
            .Include(o => o.Tickets)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

    public Task<Payment?> GetSucceededPaymentAsync(
        Guid orderId, CancellationToken cancellationToken = default) =>
        _db.Payments
            .Where(p => p.OrderId == orderId && p.Status == PaymentStatus.Succeeded)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Un autre achat a modifié le stock. On oublie tout ce qui était suivi :
            // la nouvelle tentative rechargera des données fraîches.
            _db.ChangeTracker.Clear();
            throw new ConcurrencyConflictException(
                "Le stock a été modifié par un autre achat. Réessayez.");
        }
    }
}
