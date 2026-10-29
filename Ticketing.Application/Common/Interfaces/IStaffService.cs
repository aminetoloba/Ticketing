using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ticketing.Application.DTOs.Staff;

namespace Ticketing.Application.Common.Interfaces;

public interface IStaffService
{
    Task<StaffMemberDto> AssignAsync(Guid eventId, AssignStaffRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StaffMemberDto>> GetAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid eventId, Guid staffId, CancellationToken cancellationToken = default);
}
