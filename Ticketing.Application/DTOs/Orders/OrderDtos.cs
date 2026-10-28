using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ticketing.Application.DTOs.Orders;

public record OrderLineRequest(Guid TicketTypeId, int Quantity);

public record CreateOrderRequest(Guid EventId, IReadOnlyList<OrderLineRequest> Lines);

public record OrderItemDto(Guid TicketTypeId, int Quantity, decimal UnitPrice, decimal Subtotal);

public record PayOrderRequest(string PaymentMethod);

public record OrderDto(
    Guid Id,
    Guid EventId,
    string Status,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    decimal TotalAmount,
    IReadOnlyList<OrderItemDto> Items
);
