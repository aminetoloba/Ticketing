using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Ticketing.Application.Common;
using Ticketing.Application.Common.Interfaces;
using Ticketing.Application.DTOs.Events;
using Ticketing.Application.DTOs.Users;
using Ticketing.Domain.Exceptions;
using Ticketing.Infrastructure.Persistance;

namespace Ticketing.Infrastructure.Identity;

public class UserAdminService : IUserAdminService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UserAdminService(
        UserManager<ApplicationUser> userManager, AppDbContext db, ICurrentUserService currentUser)
    {
        _userManager = userManager;
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<UserDto>> GetUsersAsync(
        UserQuery query, CancellationToken cancellationToken = default)
    {
        var users = from u in _db.Users.AsNoTracking()
                    join ur in _db.UserRoles on u.Id equals ur.UserId
                    join r in _db.Roles on ur.RoleId equals r.Id
                    select new { u.Id, Email = u.Email!, u.FullName, Role = r.Name! };

        if (!string.IsNullOrWhiteSpace(query.Role))
            users = users.Where(x => x.Role == query.Role);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{EscapeLike(query.Search.Trim())}%";
            users = users.Where(x =>
                EF.Functions.ILike(x.Email, pattern) || EF.Functions.ILike(x.FullName, pattern));
        }

        // Le tri contient aussi l'identifiant : sans lui, deux personnes de même nom
        // pourraient apparaître sur deux pages, ou aucune
        var ordered = users.OrderBy(x => x.FullName).ThenBy(x => x.Email).ThenBy(x => x.Id);

        var total = await ordered.CountAsync(cancellationToken);
        var items = await ordered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new UserDto(x.Id, x.Email, x.FullName, x.Role))
            .ToListAsync(cancellationToken);

        return new PagedResult<UserDto>(items, query.Page, query.PageSize, total);
    }

    public async Task<UserDto> ChangeRoleAsync(
        Guid userId, ChangeRoleRequest request, CancellationToken cancellationToken = default)
    {
        var actorId = _currentUser.UserId
            ?? throw new AuthException("Utilisateur non authentifié.");

        if (userId == actorId)
            throw new DomainException("Vous ne pouvez pas modifier votre propre rôle.");

        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("Utilisateur introuvable.");

        var currentRoles = await _userManager.GetRolesAsync(user);

        // Même rôle : rien à faire
        if (currentRoles.Count == 1 && currentRoles[0] == request.Role)
            return ToDto(user, request.Role);

        // On relit le nombre d'administrateurs en base : le jeton de l'appelant,
        // lui, peut dater d'avant sa propre rétrogradation
        if (currentRoles.Contains(Roles.Admin))
        {
            var admins = await _userManager.GetUsersInRoleAsync(Roles.Admin);
            if (admins.Count <= 1)
                throw new DomainException("Impossible de retirer le rôle du dernier administrateur.");
        }

        // Retirer l'ancien rôle et ajouter le nouveau : tout ou rien
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        if (currentRoles.Count > 0)
            EnsureSucceeded(await _userManager.RemoveFromRolesAsync(user, currentRoles));
        EnsureSucceeded(await _userManager.AddToRoleAsync(user, request.Role));

        await transaction.CommitAsync(cancellationToken);
        return ToDto(user, request.Role);
    }

    private static UserDto ToDto(ApplicationUser user, string role) =>
        new(user.Id, user.Email!, user.FullName, role);

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new DomainException(string.Join(" ", result.Errors.Select(e => e.Description)));
    }

    // Les caractères % et _ ont un sens spécial dans un LIKE : on les neutralise
    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
