using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ticketing.Application.Common;
using Ticketing.Application.Common.Interfaces;
using Ticketing.Application.DTOs.Orders;
using Ticketing.Domain.Enums;
using Ticketing.Domain.Exceptions;

namespace Ticketing.Application.Services;

public class RefundService : IRefundService
{
    private const int MaxAttempts = 3;

    private readonly IOrderRepository _orders;
    private readonly IEventRepository _events;
    private readonly IPaymentGateway _gateway;
    private readonly ICurrentUserService _currentUser;

    public RefundService(
        IOrderRepository orders,
        IEventRepository events,
        IPaymentGateway gateway,
        ICurrentUserService currentUser)
    {
        _orders = orders;
        _events = events;
        _gateway = gateway;
        _currentUser = currentUser;
    }

    public async Task<OrderDto> RequestRefundAsync(
        Guid orderId, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new AuthException("Utilisateur non authentifié.");

        // 1. Tous les contrôles, avant de toucher à l'argent
        var order = await _orders.GetForRefundAsync(orderId, cancellationToken);

        // La commande d'un autre : « introuvable » plutôt que « interdit »
        if (order is null || order.UserId != userId)
            throw new NotFoundException("Commande introuvable.");

        if (order.Status != OrderStatus.Paid)
            throw new DomainException(
                $"Seule une commande payée peut être remboursée (statut « {order.Status} »).");

        var @event = await _events.GetByIdAsync(order.EventId, cancellationToken)
            ?? throw new NotFoundException("Événement introuvable.");

        @event.EnsureRefundWindowOpen(DateTime.UtcNow);

        if (order.CreatedAt >= @event.PostponedAt)
            throw new DomainException(
                "Cette commande a été passée après l'annonce du report : " +
                "elle n'est pas remboursable à la demande.");

        if (order.Tickets.Any(t => t.Status == TicketStatus.Used))
            throw new DomainException(
                "Des billets de cette commande ont déjà été utilisés : remboursement impossible.");

        var payment = await _orders.GetSucceededPaymentAsync(order.Id, cancellationToken);
        if (payment?.ProviderReference is null)
            throw new DomainException("Aucun paiement à rembourser n'a été trouvé pour cette commande.");

        // 2. Le remboursement chez le fournisseur : une seule fois, jamais relancé
        var refund = await _gateway.RefundAsync(
            payment.ProviderReference, payment.Amount, cancellationToken);
        if (!refund.Succeeded)
            throw new PaymentFailedException(refund.FailureReason ?? "Le remboursement a échoué.");

        // 3. L'enregistrement, relancé en cas de conflit : l'argent est déjà rendu
        var paymentId = payment.Id;

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await ApplyRefundAsync(orderId, paymentId, cancellationToken);
            }
            catch (ConcurrencyConflictException) when (attempt < MaxAttempts)
            {
                // Un autre achat a touché au même stock : on recharge et on recommence
            }
            catch (ConcurrencyConflictException)
            {
                throw new DomainException(
                    "Votre remboursement a été effectué mais n'a pas pu être enregistré. " +
                    "Contactez le support.");
            }
        }
    }

    private async Task<OrderDto> ApplyRefundAsync(
        Guid orderId, Guid paymentId, CancellationToken cancellationToken)
    {
        // Données rechargées à chaque tentative : après un conflit, EF a tout oublié
        var order = await _orders.GetForRefundAsync(orderId, cancellationToken)
            ?? throw new NotFoundException("Commande introuvable.");

        // Les types de billets arrivent suivis par EF avec l'événement
        var @event = await _events.GetByIdAsync(order.EventId, cancellationToken)
            ?? throw new NotFoundException("Événement introuvable.");

        var payment = await _orders.GetPaymentAsync(paymentId, cancellationToken)
            ?? throw new NotFoundException("Paiement introuvable.");

        if (order.Status != OrderStatus.Paid)
            throw new DomainException(
                "La commande a changé pendant le remboursement. " +
                "Contactez le support si vous n'avez pas été remboursé.");

        payment.MarkAsRefunded();
        order.Cancel(); // annule aussi les billets

        // L'événement continue : les places remises sont de nouveau en vente
        foreach (var item in order.Items)
        {
            var ticketType = @event.TicketTypes.FirstOrDefault(t => t.Id == item.TicketTypeId);
            ticketType?.Release(item.Quantity);
        }

        // Une seule transaction : paiement, commande, billets et stock, ou rien
        await _orders.SaveChangesAsync(cancellationToken);
        return OrderService.ToDto(order);
    }
}
