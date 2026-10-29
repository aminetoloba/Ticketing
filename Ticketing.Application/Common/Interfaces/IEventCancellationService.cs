using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ticketing.Application.DTOs.Events;

namespace Ticketing.Application.Common.Interfaces;

public interface IEventCancellationService
{
    Task<CancelEventResultDto> CancelAsync(Guid eventId, CancellationToken cancellationToken = default);
}
