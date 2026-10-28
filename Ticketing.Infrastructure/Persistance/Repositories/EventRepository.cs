using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ticketing.Application.Common.Interfaces;
using Ticketing.Application.DTOs.Events;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Enums;

namespace Ticketing.Infrastructure.Persistance.Repositories;

public class EventRepository : IEventRepository
{
    private readonly AppDbContext _db;

    public EventRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Events
            .Include(e => e.TicketTypes)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<Guid?> GetOrganizerIdAsync(
        Guid eventId, CancellationToken cancellationToken = default)
    {
        // On ne lit que la colonne nécessaire, pas tout l'événement
        return await _db.Events
            .Where(e => e.Id == eventId)
            .Select(e => (Guid?)e.OrganizerId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Event> Items, int TotalCount)> SearchPublicAsync(
        EventQuery query, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // Visibles du public : Publié ou Reporté, et pas encore terminés
        var events = _db.Events
            .AsNoTracking()
            .Where(e => (e.Status == EventStatus.Published || e.Status == EventStatus.Postponed)
                        && e.EndDate >= now);

        var ordered = ApplyFilters(events, query)
            .OrderBy(e => e.StartDate)
            .ThenBy(e => e.Id);

        return await PageAsync(ordered, query, cancellationToken);
    }

    public async Task<(IReadOnlyList<Event> Items, int TotalCount)> SearchByOrganizerAsync(
        Guid organizerId, EventQuery query, CancellationToken cancellationToken = default)
    {
        // Tous les statuts, brouillons compris
        var events = _db.Events
            .AsNoTracking()
            .Where(e => e.OrganizerId == organizerId);

        var ordered = ApplyFilters(events, query)
            .OrderByDescending(e => e.CreatedAt)
            .ThenBy(e => e.Id);

        return await PageAsync(ordered, query, cancellationToken);
    }

    public async Task<IReadOnlyList<TicketType>> GetTicketTypesAsync(
        Guid eventId, CancellationToken cancellationToken = default) =>
        await _db.TicketTypes
            .AsNoTracking()
            .Where(t => t.EventId == eventId)
            .OrderBy(t => t.Price)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Event @event, CancellationToken cancellationToken = default) =>
        await _db.Events.AddAsync(@event, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);

    private static IQueryable<Event> ApplyFilters(IQueryable<Event> events, EventQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{EscapeLike(query.Search.Trim())}%";
            events = events.Where(e =>
                EF.Functions.ILike(e.Title, pattern) ||
                EF.Functions.ILike(e.Description, pattern));
        }

        if (!string.IsNullOrWhiteSpace(query.Location))
        {
            var pattern = $"%{EscapeLike(query.Location.Trim())}%";
            events = events.Where(e => EF.Functions.ILike(e.Location, pattern));
        }

        // Événements qui se terminent à partir de « From »
        if (query.From.HasValue)
        {
            var from = query.From.Value.UtcDateTime;
            events = events.Where(e => e.EndDate >= from);
        }

        // Événements qui commencent jusqu'à « To »
        if (query.To.HasValue)
        {
            var to = query.To.Value.UtcDateTime;
            events = events.Where(e => e.StartDate <= to);
        }

        return events;
    }

    private static async Task<(IReadOnlyList<Event> Items, int TotalCount)> PageAsync(
        IOrderedQueryable<Event> ordered, EventQuery query, CancellationToken cancellationToken)
    {
        var total = await ordered.CountAsync(cancellationToken);

        var items = await ordered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    // Les caractères % et _ ont un sens spécial dans un LIKE :
    // on les neutralise pour que la recherche reste littérale
    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
