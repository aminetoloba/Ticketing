using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ticketing.Application.Common;
using Ticketing.Application.Common.Interfaces;
using Ticketing.Application.DTOs.Orders;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Enums;
using Ticketing.Domain.Exceptions;

namespace Ticketing.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IOrderRepository _orders;
    private readonly IPaymentGateway _gateway;
    private readonly ICurrentUserService _currentUser;

    public PaymentService(
        IOrderRepository orders, IPaymentGateway gateway, ICurrentUserService currentUser)
    {
        _orders = orders;
        _gateway = gateway;
        _currentUser = currentUser;
    }

    public async Task<OrderDto> PayOrderAsync(
        Guid orderId, PayOrderRequest request, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new AuthException("Utilisateur non authentifié.");

        var order = await _orders.GetByIdForPaymentAsync(orderId, cancellationToken);

        // La commande d'un autre : « introuvable » plutôt que « interdit »,
        // pour ne pas révéler qu'elle existe
        if (order is null || order.UserId != userId)
            throw new NotFoundException("Commande introuvable.");

        // Double clic sur « Payer » : on renvoie la commande déjà payée, sans repayer
        if (order.Status == OrderStatus.Paid)
            return OrderService.ToDto(order);

        if (order.Status != OrderStatus.Pending)
            throw new DomainException($"Impossible de payer une commande au statut « {order.Status} ».");

        if (order.IsExpired(DateTime.UtcNow))
            throw new DomainException("La réservation a expiré. Recommencez votre commande.");

        // On trace la tentative avant d'appeler le fournisseur
        var payment = Payment.Create(order.Id, order.TotalAmount);
        await _orders.AddPaymentAsync(payment, cancellationToken);
        await _orders.SaveChangesAsync(cancellationToken);

        // Le montant vient de la commande en base, jamais de la requête
        var result = await _gateway.ChargeAsync(
            order.Id, order.TotalAmount, request.PaymentMethod, cancellationToken);

        if (!result.Succeeded)
        {
            payment.MarkAsFailed();
            await _orders.SaveChangesAsync(cancellationToken);
            throw new PaymentFailedException(result.FailureReason ?? "Le paiement a été refusé.");
        }

        try
        {
            payment.MarkAsSucceeded(result.ProviderReference!);
            order.MarkAsPaid(); // génère les billets

            // Un seul enregistrement : paiement, commande et billets, ou rien
            await _orders.SaveChangesAsync(cancellationToken);
            return OrderService.ToDto(order);
        }
        catch (ConcurrencyConflictException)
        {
            // La commande a changé pendant le paiement : l'argent est encaissé,
            // mais la commande n'est plus payable. On rembourse.
            await RefundAfterConflictAsync(
                payment.Id, result.ProviderReference!, order.TotalAmount, cancellationToken);

            throw new DomainException(
                "La commande a été modifiée pendant le paiement (expirée ou déjà payée). " +
                "Votre paiement a été remboursé.");
        }
    }

    private async Task RefundAfterConflictAsync(
        Guid paymentId, string providerReference, decimal amount, CancellationToken cancellationToken)
    {
        var refund = await _gateway.RefundAsync(providerReference, amount, cancellationToken);

        // Le conflit a vidé le suivi EF : on recharge le paiement
        var payment = await _orders.GetPaymentAsync(paymentId, cancellationToken)
            ?? throw new NotFoundException("Paiement introuvable.");

        payment.MarkAsSucceeded(providerReference);
        if (refund.Succeeded)
            payment.MarkAsRefunded();

        await _orders.SaveChangesAsync(cancellationToken);

        if (!refund.Succeeded)
            throw new DomainException(
                "Le remboursement automatique a échoué. Contactez le support.");
    }
}
