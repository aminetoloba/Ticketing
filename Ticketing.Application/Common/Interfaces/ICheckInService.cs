using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ticketing.Application.DTOs.CheckIn;

namespace Ticketing.Application.Common.Interfaces;

public interface ICheckInService
{
    Task<CheckInResultDto> CheckInAsync(
        Guid eventId, CheckInRequest request, CancellationToken cancellationToken = default);
}
