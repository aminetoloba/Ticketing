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

public class OrderService : IOrderService
{
    private const int MaxAttempts = 3;
    private static readonly TimeSpan ReservationDuration = TimeSpan.FromMinutes(10);

    private readonly IEventRepository _events;
    private readonly IOrderRepository _orders;
    private readonly ICurrentUserService _currentUser;

    public OrderService(
        IEventRepository events,
        IOrderRepository orders,
        ICurrentUserService currentUser)
    {
        _events = events;
        _orders = orders;
        _currentUser = currentUser;
    }

    public async Task<OrderDto> CreateAsync(
        CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new AuthException("Utilisateur non authentifié.");

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await TryCreateAsync(userId, request, cancellationToken);
            }
            catch (ConcurrencyConflictException) when (attempt < MaxAttempts)
            {
                // Un autre achat a pris des places entre notre lecture et notre écriture.
                // On relit le stock à jour et on recommence : s'il ne reste plus assez
                // de places, TicketType.Reserve répondra « plus de places ».
            }
        }
    }

    public async Task<IReadOnlyList<OrderDto>> GetMineAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new AuthException("Utilisateur non authentifié.");

        var orders = await _orders.GetByUserAsync(userId, cancellationToken);
        return orders.Select(ToDto).ToList();
    }

    private async Task<OrderDto> TryCreateAsync(
        Guid userId, CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var @event = await _events.GetByIdAsync(request.EventId, cancellationToken)
            ?? throw new NotFoundException("Événement introuvable.");

        // Un brouillon n'existe pas pour le public
        if (@event.Status == EventStatus.Draft)
            throw new NotFoundException("Événement introuvable.");

        if (@event.Status is not (EventStatus.Published or EventStatus.Postponed) || @event.EndDate <= now)
            throw new DomainException("Cet événement n'est pas ouvert à la vente.");

        var lines = new List<(TicketType TicketType, int Quantity)>();

        foreach (var line in request.Lines)
        {
            var ticketType = @event.TicketTypes.FirstOrDefault(t => t.Id == line.TicketTypeId)
                ?? throw new DomainException("Ce type de billet n'existe pas pour cet événement.");

            // Réserve le stock : lève InsufficientStockException s'il en manque
            ticketType.Reserve(line.Quantity);
            lines.Add((ticketType, line.Quantity));
        }

        var order = Order.Create(userId, @event.Id, lines, now, ReservationDuration);

        // Une seule transaction : la commande et le stock sont enregistrés ensemble,
        // ou rien du tout
        await _orders.AddAsync(order, cancellationToken);
        await _orders.SaveChangesAsync(cancellationToken);

        return ToDto(order);
    }

    internal static OrderDto ToDto(Order o) => new(
        o.Id,
        o.EventId,
        o.Status.ToString(),
        o.CreatedAt,
        o.ExpiresAt,
        o.TotalAmount,
        o.Items
            .Select(i => new OrderItemDto(i.TicketTypeId, i.Quantity, i.UnitPrice, i.Subtotal))
            .ToList());
}
