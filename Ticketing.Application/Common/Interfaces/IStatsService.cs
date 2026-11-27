using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ticketing.Application.DTOs.Stats;

namespace Ticketing.Application.Common.Interfaces;

public interface IStatsService
{
    Task<EventStatsDto> GetAsync(Guid eventId, CancellationToken cancellationToken = default);
}
