using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ticketing.Application.Common;
using Ticketing.Application.Common.Interfaces;
using Ticketing.Application.DTOs.Events;
using Ticketing.Domain.Enums;
using Ticketing.Domain.Exceptions;

namespace Ticketing.Application.Services;

public class EventCancellationService : IEventCancellationService
{
    private const int MaxAttempts = 3;

    private enum RefundOutcome { Refunded, AlreadyHandled, Failed }

    private readonly IEventRepository _events;
    private readonly IOrderRepository _orders;
    private readonly ITicketRepository _tickets;
    private readonly IPaymentGateway _gateway;

    public EventCancellationService(
        IEventRepository events,
        IOrderRepository orders,
        ITicketRepository tickets,
        IPaymentGateway gateway)
    {
        _events = events;
        _orders = orders;
        _tickets = tickets;
        _gateway = gateway;
    }

    public async Task<CancelEventResultDto> CancelAsync(
        Guid eventId, CancellationToken cancellationToken = default)
    {
        var @event = await _events.GetByIdAsync(eventId, cancellationToken)
            ?? throw new NotFoundException("Événement introuvable.");

        // Étape 1 : l'événement passe à « Annulé ». S'il l'est déjà, on reprend
        // simplement les remboursements restants (rappel après un échec partiel).
        if (@event.Status != EventStatus.Cancelled)
        {
            if (await _tickets.AnyUsedForEventAsync(eventId, cancellationToken))
                throw new DomainException(
                    "Des billets ont déjà été scannés : l'événement ne peut plus être annulé.");

            @event.Cancel();
            await _events.SaveChangesAsync(cancellationToken);
        }

        // Étape 2 : les commandes en attente ne pourront plus être payées
        var cancelledPending = await CancelPendingOrdersAsync(eventId, cancellationToken);

        // Étape 3 : on rembourse chaque commande payée, une transaction par commande.
        // Cette liste est lue après l'étape 2 : une commande payée entre-temps y figure.
        var paidIds = await _orders.GetPaidOrderIdsByEventAsync(eventId, cancellationToken);

        var refunded = 0;
        var failed = new List<Guid>();

        foreach (var orderId in paidIds)
        {
            switch (await TryRefundAsync(orderId, cancellationToken))
            {
                case RefundOutcome.Refunded: refunded++; break;
                case RefundOutcome.Failed: failed.Add(orderId); break;
            }
        }

        return new CancelEventResultDto(
            EventService.ToDto(@event), cancelledPending, refunded, failed);
    }

    private async Task<int> CancelPendingOrdersAsync(Guid eventId, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            var pending = await _orders.GetPendingByEventAsync(eventId, cancellationToken);
            if (pending.Count == 0)
                return 0;

            foreach (var order in pending)
                order.Cancel();

            try
            {
                await _orders.SaveChangesAsync(cancellationToken);
                return pending.Count;
            }
            catch (ConcurrencyConflictException) when (attempt < MaxAttempts)
            {
                // Une commande a été payée pendant l'opération : on recharge et on recommence
            }
        }
    }

    private async Task<RefundOutcome> TryRefundAsync(Guid orderId, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _orders.GetForRefundAsync(orderId, cancellationToken);
            if (order is null || order.Status != OrderStatus.Paid)
                return RefundOutcome.AlreadyHandled;

            // Billets déjà scannés : on ne rembourse pas automatiquement, à traiter à la main
            if (order.Tickets.Any(t => t.Status == TicketStatus.Used))
                return RefundOutcome.Failed;

            var payment = await _orders.GetSucceededPaymentAsync(orderId, cancellationToken);
            if (payment?.ProviderReference is null)
                return RefundOutcome.Failed;

            var refund = await _gateway.RefundAsync(
                payment.ProviderReference, payment.Amount, cancellationToken);
            if (!refund.Succeeded)
                return RefundOutcome.Failed;

            payment.MarkAsRefunded();
            order.Cancel(); // annule aussi les billets de la commande

            await _orders.SaveChangesAsync(cancellationToken);
            return RefundOutcome.Refunded;
        }
        catch (Exception ex) when (ex is ConcurrencyConflictException or DomainException)
        {
            // Une autre opération a touché la commande : on la signale, un rappel la retraitera
            return RefundOutcome.Failed;
        }
    }
}
