using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ticketing.Application.DTOs.Staff;
using Ticketing.Domain.Entities;

namespace Ticketing.Application.Common.Interfaces;

public interface IStaffRepository
{
    // Renvoie le compte seulement s'il porte le rôle Staff
    Task<StaffUserInfo?> FindStaffUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> IsAssignedAsync(Guid staffId, Guid eventId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StaffMemberDto>> GetByEventAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<StaffAssignment?> GetAssignmentAsync(Guid eventId, Guid staffId, CancellationToken cancellationToken = default);

    Task AddAsync(StaffAssignment assignment, CancellationToken cancellationToken = default);
    void Remove(StaffAssignment assignment);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
