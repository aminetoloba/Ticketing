using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ticketing.Application.Common;
using Ticketing.Application.Common.Interfaces;
using Ticketing.Application.DTOs.Staff;
using Ticketing.Domain.Entities;

namespace Ticketing.Infrastructure.Persistance.Repositories;

public class StaffRepository : IStaffRepository
{
    private readonly AppDbContext _db;

    public StaffRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<StaffUserInfo?> FindStaffUserByEmailAsync(
        string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToUpperInvariant();

        return (from u in _db.Users
                join ur in _db.UserRoles on u.Id equals ur.UserId
                join r in _db.Roles on ur.RoleId equals r.Id
                where u.NormalizedEmail == normalized && r.Name == Roles.Staff
                select new StaffUserInfo(u.Id, u.Email!, u.FullName))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> IsAssignedAsync(
        Guid staffId, Guid eventId, CancellationToken cancellationToken = default) =>
        _db.StaffAssignments.AnyAsync(
            s => s.StaffId == staffId && s.EventId == eventId, cancellationToken);

    public async Task<IReadOnlyList<StaffMemberDto>> GetByEventAsync(
        Guid eventId, CancellationToken cancellationToken = default) =>
        await (from s in _db.StaffAssignments
               join u in _db.Users on s.StaffId equals u.Id
               where s.EventId == eventId
               orderby s.CreatedAt
               select new StaffMemberDto(s.StaffId, u.Email!, u.FullName, s.CreatedAt))
            .ToListAsync(cancellationToken);

    public Task<StaffAssignment?> GetAssignmentAsync(
        Guid eventId, Guid staffId, CancellationToken cancellationToken = default) =>
        _db.StaffAssignments.FirstOrDefaultAsync(
            s => s.EventId == eventId && s.StaffId == staffId, cancellationToken);

    public async Task AddAsync(
        StaffAssignment assignment, CancellationToken cancellationToken = default) =>
        await _db.StaffAssignments.AddAsync(assignment, cancellationToken);

    // L'intercepteur d'audit transforme cette suppression en suppression logique
    public void Remove(StaffAssignment assignment) =>
        _db.StaffAssignments.Remove(assignment);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
