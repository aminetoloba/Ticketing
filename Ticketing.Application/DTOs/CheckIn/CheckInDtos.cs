using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ticketing.Application.DTOs.CheckIn;

public record CheckInRequest(string Code);

public record CheckInResultDto(
    Guid TicketId,
    string TicketTypeName,
    string Status,
    DateTime UsedAt
);
