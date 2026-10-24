using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ticketing.Domain.Common;
using Ticketing.Domain.Exceptions;

namespace Ticketing.Domain.Entities;

public class StaffAssignment : AuditableEntity
{
    private StaffAssignment() { }

    public Guid Id { get; private set; }
    public Guid StaffId { get; private set; }
    public Guid EventId { get; private set; }

    public static StaffAssignment Create(Guid staffId, Guid eventId)
    {
        if (staffId == Guid.Empty)
            throw new DomainException("Le membre du staff est obligatoire.");
        if (eventId == Guid.Empty)
            throw new DomainException("L'événement est obligatoire.");

        return new StaffAssignment
        {
            Id = Guid.NewGuid(),
            StaffId = staffId,
            EventId = eventId
        };
    }
}
