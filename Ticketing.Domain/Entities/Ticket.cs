using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Ticketing.Domain.Enums;
using Ticketing.Domain.Exceptions;
using Ticketing.Domain.Common;

namespace Ticketing.Domain.Entities;

public class Ticket : AuditableEntity
{
    private Ticket() { }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid EventId { get; private set; }
    public Guid TicketTypeId { get; private set; }

    // Code du QR : aléatoire, impossible à deviner
    public string Code { get; private set; } = string.Empty;
    public TicketStatus Status { get; private set; }

    public static Ticket Create(Guid orderId, Guid eventId, Guid ticketTypeId)
    {
        return new Ticket
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            EventId = eventId,
            TicketTypeId = ticketTypeId,
            Code = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)),
            Status = TicketStatus.Valid
        };
    }

    public void MarkAsUsed()
    {
        if (Status == TicketStatus.Used)
            throw new DomainException("Ce billet a déjà été utilisé.");
        if (Status == TicketStatus.Cancelled)
            throw new DomainException("Ce billet est annulé.");

        Status = TicketStatus.Used;
    }

    public void Cancel()
    {
        if (Status == TicketStatus.Used)
            throw new DomainException("Impossible d'annuler un billet déjà utilisé.");

        Status = TicketStatus.Cancelled;
    }
}
