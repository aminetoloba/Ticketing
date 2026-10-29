using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ticketing.Domain.Exceptions;

public class TicketAlreadyUsedException : DomainException
{
    public TicketAlreadyUsedException(string message) : base(message)
    {
    }
}
