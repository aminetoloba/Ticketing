using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ticketing.Application.Common;
using Ticketing.Application.Common.Interfaces;
using Ticketing.Application.DTOs.Staff;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Enums;
using Ticketing.Domain.Exceptions;

namespace Ticketing.Application.Services;

public class StaffService : IStaffService
{
    private readonly IEventRepository _events;
    private readonly IStaffRepository _staff;

    public StaffService(IEventRepository events, IStaffRepository staff)
    {
        _events = events;
        _staff = staff;
    }

    public async Task<StaffMemberDto> AssignAsync(
        Guid eventId, AssignStaffRequest request, CancellationToken cancellationToken = default)
    {
        var @event = await _events.GetByIdAsync(eventId, cancellationToken)
            ?? throw new NotFoundException("Événement introuvable.");

        if (@event.Status is EventStatus.Cancelled or EventStatus.Completed)
            throw new DomainException(
                $"Impossible d'affecter du staff à un événement au statut « {@event.Status} ».");

        // Même message pour « email inconnu » et « compte non Staff » :
        // on ne révèle pas quels comptes existent
        var user = await _staff.FindStaffUserByEmailAsync(request.Email, cancellationToken)
            ?? throw new DomainException("Aucun compte Staff ne correspond à cet email.");

        if (await _staff.IsAssignedAsync(user.Id, eventId, cancellationToken))
            throw new DomainException("Ce membre du staff est déjà affecté à cet événement.");

        var assignment = StaffAssignment.Create(user.Id, eventId);
        await _staff.AddAsync(assignment, cancellationToken);
        await _staff.SaveChangesAsync(cancellationToken);

        return new StaffMemberDto(user.Id, user.Email, user.FullName, assignment.CreatedAt);
    }

    public async Task<IReadOnlyList<StaffMemberDto>> GetAsync(
        Guid eventId, CancellationToken cancellationToken = default)
    {
        if (await _events.GetOrganizerIdAsync(eventId, cancellationToken) is null)
            throw new NotFoundException("Événement introuvable.");

        return await _staff.GetByEventAsync(eventId, cancellationToken);
    }

    public async Task RemoveAsync(
        Guid eventId, Guid staffId, CancellationToken cancellationToken = default)
    {
        var assignment = await _staff.GetAssignmentAsync(eventId, staffId, cancellationToken)
            ?? throw new NotFoundException("Affectation introuvable.");

        _staff.Remove(assignment);
        await _staff.SaveChangesAsync(cancellationToken);
    }
}
