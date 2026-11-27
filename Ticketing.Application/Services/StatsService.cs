using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ticketing.Application.Common;
using Ticketing.Application.Common.Interfaces;
using Ticketing.Application.DTOs.Stats;

namespace Ticketing.Application.Services;

public class StatsService : IStatsService
{
    // Au-delà, on ne comble pas les jours sans vente : le graphique serait illisible
    private const int MaxFilledDays = 400;

    private readonly IEventRepository _events;
    private readonly IStatsRepository _stats;

    public StatsService(IEventRepository events, IStatsRepository stats)
    {
        _events = events;
        _stats = stats;
    }

    public async Task<EventStatsDto> GetAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        if (await _events.GetOrganizerIdAsync(eventId, cancellationToken) is null)
            throw new NotFoundException("Événement introuvable.");

        // Les requêtes passent l'une après l'autre : un DbContext n'en accepte pas deux en même temps
        var types = await _stats.GetTicketTypesAsync(eventId, cancellationToken);
        var paid = (await _stats.GetPaidTicketCountsAsync(eventId, cancellationToken))
            .ToDictionary(x => x.TicketTypeId);
        var revenueByType = (await _stats.GetPaidRevenueByTypeAsync(eventId, cancellationToken))
            .ToDictionary(x => x.TicketTypeId);
        var pending = (await _stats.GetPendingByTypeAsync(eventId, DateTime.UtcNow, cancellationToken))
            .ToDictionary(x => x.TicketTypeId);
        var dailyRows = await _stats.GetDailySalesAsync(eventId, cancellationToken);
        var refunded = await _stats.GetRefundedAmountAsync(eventId, cancellationToken);

        var byType = types.Select(t =>
        {
            var sold = paid.TryGetValue(t.Id, out var p) ? p.Sold : 0;
            var reserved = pending.TryGetValue(t.Id, out var r) ? r.Quantity : 0;
            var revenue = revenueByType.TryGetValue(t.Id, out var a) ? a.Amount : 0m;
            return new TicketTypeStatsDto(t.Id, t.Name, t.Price, t.Capacity, sold, reserved, revenue, Rate(sold, t.Capacity));
        }).ToList();

        var capacity = byType.Sum(t => t.Capacity);
        var sold = byType.Sum(t => t.Sold);
        var reservedTotal = byType.Sum(t => t.Reserved);
        var checkedIn = paid.Values.Sum(x => x.Used);

        return new EventStatsDto(
            eventId,
            capacity,
            sold,
            reservedTotal,
            Math.Max(0, capacity - sold - reservedTotal),
            Rate(sold, capacity),
            byType.Sum(t => t.Revenue),
            refunded,
            checkedIn,
            Rate(checkedIn, sold),
            byType,
            FillMissingDays(dailyRows));
    }

    private static decimal Rate(int part, int total) =>
        total == 0 ? 0m : Math.Round((decimal)part / total, 4);

    // Ajoute les jours sans vente (à zéro) entre la première et la dernière vente,
    // pour que le graphique garde un axe du temps régulier
    private static List<DailySalesDto> FillMissingDays(IReadOnlyList<DailyRow> rows)
    {
        var byDay = rows.ToDictionary(r => DateOnly.FromDateTime(r.Day));
        if (byDay.Count == 0)
            return [];

        var first = byDay.Keys.Min();
        var last = byDay.Keys.Max();
        var result = new List<DailySalesDto>();

        if (last.DayNumber - first.DayNumber > MaxFilledDays)
            return byDay.OrderBy(x => x.Key)
                .Select(x => new DailySalesDto(x.Key, x.Value.Orders, x.Value.Tickets, x.Value.Revenue))
                .ToList();

        for (var day = first; day <= last; day = day.AddDays(1))
        {
            result.Add(byDay.TryGetValue(day, out var row)
                ? new DailySalesDto(day, row.Orders, row.Tickets, row.Revenue)
                : new DailySalesDto(day, 0, 0, 0m));
        }

        return result;
    }
}