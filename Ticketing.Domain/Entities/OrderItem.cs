using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ticketing.Domain.Common;

namespace Ticketing.Domain.Entities;

public class OrderItem : AuditableEntity
{
    private OrderItem() { }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid TicketTypeId { get; private set; }
    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal Subtotal => UnitPrice * Quantity;

    public static OrderItem Create(Guid orderId, Guid ticketTypeId, int quantity, decimal unitPrice)
    {
        return new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            TicketTypeId = ticketTypeId,
            Quantity = quantity,
            UnitPrice = unitPrice
        };
    }
}
