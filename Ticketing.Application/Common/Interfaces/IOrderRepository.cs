using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ticketing.Domain.Entities;

namespace Ticketing.Application.Common.Interfaces;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    // Commande suivie par EF, avec ses lignes
    Task<Order?> GetByIdForPaymentAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task AddPaymentAsync(Payment payment, CancellationToken cancellationToken = default);
    Task<Payment?> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetPendingByEventAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetPaidOrderIdsByEventAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<Order?> GetForRefundAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<Payment?> GetSucceededPaymentAsync(Guid orderId, CancellationToken cancellationToken = default);
    // Commandes en attente dont le délai est dépassé (suivies, avec leurs lignes)
    Task<IReadOnlyList<Order>> GetExpiredPendingAsync(
        DateTime utcNow, int take, CancellationToken cancellationToken = default);

    // Types de billets suivis par EF, pour pouvoir leur rendre des places
    Task<IReadOnlyList<TicketType>> GetTicketTypesForUpdateAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    // Lève ConcurrencyConflictException si le stock a été modifié entre-temps
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
