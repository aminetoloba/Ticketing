using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ticketing.Application.Common.Interfaces;

namespace Ticketing.Application.Services;

public class OrderExpirationService : IOrderExpirationService
{
    // Taille d'un lot : évite de charger des milliers de commandes d'un coup
    private const int BatchSize = 50;

    private readonly IOrderRepository _orders;

    public OrderExpirationService(IOrderRepository orders)
    {
        _orders = orders;
    }

    public async Task<int> ExpireOverdueOrdersAsync(
        DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var orders = await _orders.GetExpiredPendingAsync(utcNow, BatchSize, cancellationToken);
        if (orders.Count == 0)
            return 0;

        var ticketTypeIds = orders
            .SelectMany(o => o.Items)
            .Select(i => i.TicketTypeId)
            .Distinct()
            .ToList();

        var ticketTypes = (await _orders.GetTicketTypesForUpdateAsync(ticketTypeIds, cancellationToken))
            .ToDictionary(t => t.Id);

        foreach (var order in orders)
        {
            order.Expire();

            foreach (var item in order.Items)
            {
                if (ticketTypes.TryGetValue(item.TicketTypeId, out var ticketType))
                    ticketType.Release(item.Quantity);
            }
        }

        // Une seule transaction : les commandes passent à « Expirée »
        // et le stock est rendu en même temps, ou rien ne change
        await _orders.SaveChangesAsync(cancellationToken);
        return orders.Count;
    }
}
