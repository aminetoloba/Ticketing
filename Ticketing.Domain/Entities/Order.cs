using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ticketing.Domain.Enums;
using Ticketing.Domain.Exceptions;
using Ticketing.Domain.Common;

namespace Ticketing.Domain.Entities;

public class Order : AuditableEntity
{
    private Order() { }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid EventId { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public decimal TotalAmount { get; private set; }

    // Concurrence optimiste : empêche qu'un paiement et une expiration
    // modifient la même commande en même temps
    public uint RowVersion { get; private set; }

    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items;

    private readonly List<Ticket> _tickets = new();
    public IReadOnlyCollection<Ticket> Tickets => _tickets;

    // Crée une commande « En attente ». Le stock est réservé par l'appelant
    // (via TicketType.Reserve), pas ici.
    public static Order Create(
        Guid userId,
        Guid eventId,
        IEnumerable<(TicketType TicketType, int Quantity)> lines,
        DateTime utcNow,
        TimeSpan reservationDuration)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EventId = eventId,
            Status = OrderStatus.Pending,
            ExpiresAt = utcNow + reservationDuration
        };

        foreach (var (ticketType, quantity) in lines)
        {
            if (ticketType.EventId != eventId)
                throw new DomainException("Ce type de billet n'appartient pas à cet événement.");
            if (quantity <= 0)
                throw new DomainException("La quantité doit être supérieure à zéro.");

            order._items.Add(OrderItem.Create(order.Id, ticketType.Id, quantity, ticketType.Price));
        }

        if (order._items.Count == 0)
            throw new DomainException("Une commande doit contenir au moins une ligne.");

        order.TotalAmount = order._items.Sum(i => i.Subtotal);
        return order;
    }

    public bool IsExpired(DateTime utcNow) =>
        Status == OrderStatus.Pending && utcNow >= ExpiresAt;

    // Paiement réussi : les billets sont générés à cet instant
    public void MarkAsPaid()
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException($"Impossible de payer une commande au statut « {Status} ».");

        Status = OrderStatus.Paid;

        foreach (var item in _items)
            for (var i = 0; i < item.Quantity; i++)
                _tickets.Add(Ticket.Create(Id, EventId, item.TicketTypeId));
    }

    // Délai dépassé sans paiement
    public void Expire()
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException($"Impossible d'expirer une commande au statut « {Status} ».");

        Status = OrderStatus.Expired;
    }

    // Annulation (échec de paiement, annulation de l'événement, remboursement)
    public void Cancel()
    {
        if (Status is not (OrderStatus.Pending or OrderStatus.Paid))
            throw new DomainException($"Impossible d'annuler une commande au statut « {Status} ».");

        Status = OrderStatus.Cancelled;

        foreach (var ticket in _tickets)
            ticket.Cancel();
    }
}
