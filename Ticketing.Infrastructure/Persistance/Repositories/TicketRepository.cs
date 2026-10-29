using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ticketing.Application.Common;
using Ticketing.Application.Common.Interfaces;
using Ticketing.Application.DTOs.Tickets;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Enums;

namespace Ticketing.Infrastructure.Persistance.Repositories;

public class TicketRepository : ITicketRepository
{
    private readonly AppDbContext _db;

    public TicketRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TicketDto>> GetByUserAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        // Un billet appartient à l'utilisateur de sa commande
        var rows = await (
            from t in _db.Tickets
            join o in _db.Orders on t.OrderId equals o.Id
            join e in _db.Events on t.EventId equals e.Id
            join tt in _db.TicketTypes on t.TicketTypeId equals tt.Id
            where o.UserId == userId
            orderby e.StartDate, t.CreatedAt, t.Id
            select new
            {
                t.Id,
                t.OrderId,
                t.EventId,
                EventTitle = e.Title,
                EventLocation = e.Location,
                EventStartDate = e.StartDate,
                EventEndDate = e.EndDate,
                EventStatus = e.Status,
                TicketTypeName = tt.Name,
                TicketStatus = t.Status,
                t.Code
            }).ToListAsync(cancellationToken);

        // Les statuts (enums) sont convertis en texte ici, en mémoire,
        // pour ne pas dépendre de la traduction SQL
        return rows
            .Select(r => new TicketDto(
                r.Id, r.OrderId, r.EventId,
                r.EventTitle, r.EventLocation,
                r.EventStartDate, r.EventEndDate,
                r.EventStatus.ToString(),
                r.TicketTypeName,
                r.TicketStatus.ToString(),
                r.Code))
            .ToList();
    }

    public Task<Ticket?> GetOwnedAsync(
        Guid ticketId, Guid userId, CancellationToken cancellationToken = default) =>
        (from t in _db.Tickets
         join o in _db.Orders on t.OrderId equals o.Id
         where t.Id == ticketId && o.UserId == userId
         select t)
        .AsNoTracking()
        .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> AnyUsedForEventAsync(Guid eventId, CancellationToken cancellationToken = default) =>
    _db.Tickets.AnyAsync(
        t => t.EventId == eventId && t.Status == TicketStatus.Used, cancellationToken);

    public Task<Ticket?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
    _db.Tickets.FirstOrDefaultAsync(t => t.Code == code, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _db.ChangeTracker.Clear();
            throw new ConcurrencyConflictException(
                "Le billet a été modifié par une autre opération.");
        }
    }
}
