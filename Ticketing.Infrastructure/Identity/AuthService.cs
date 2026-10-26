using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Ticketing.Application.Common;
using Ticketing.Application.Common.Interfaces;
using Ticketing.Application.DTOs.Auth;
using Ticketing.Domain.Entities;
using Ticketing.Infrastructure.Persistance;

namespace Ticketing.Infrastructure.Identity;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        AppDbContext db,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _db = db;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request, CancellationToken cancellationToken = default)
    {
        // Double sécurité : le validateur filtre déjà, mais le service ne doit pas
        // dépendre de lui pour refuser un rôle Admin ou Staff
        if (request.Role is not (Roles.Participant or Roles.Organizer))
            throw new AuthException("Le rôle doit être Participant ou Organizer.", isUnauthorized: false);

        var email = request.Email.Trim();

        if (await _userManager.FindByEmailAsync(email) is not null)
            throw new AuthException("Cet email est déjà utilisé.", isUnauthorized: false);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FullName = request.FullName.Trim()
        };

        var created = await _userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded)
            throw new AuthException(string.Join(" ", created.Errors.Select(e => e.Description)), isUnauthorized: false);

        var roleAdded = await _userManager.AddToRoleAsync(user, request.Role);
        if (!roleAdded.Succeeded)
        {
            // On évite de laisser un compte sans rôle
            await _userManager.DeleteAsync(user);
            throw new AuthException(string.Join(" ", roleAdded.Errors.Select(e => e.Description)), isUnauthorized: false);
        }

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request, CancellationToken cancellationToken = default)
    {
        // Même message pour « email inconnu » et « mauvais mot de passe » :
        // on ne révèle pas quels emails existent
        const string invalidCredentials = "Email ou mot de passe incorrect.";

        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
            throw new AuthException(invalidCredentials);

        if (await _userManager.IsLockedOutAsync(user))
            throw new AuthException("Compte temporairement verrouillé. Réessayez plus tard.");

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            await _userManager.AccessFailedAsync(user);
            throw new AuthException(invalidCredentials);
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(
        RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var hash = _tokenService.HashToken(request.RefreshToken);

        var stored = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken)
            ?? throw new AuthException("Refresh token invalide.");

        // Un token déjà révoqué qui revient a peut-être été volé :
        // on coupe toutes les sessions de cet utilisateur
        if (stored.RevokedAt is not null)
        {
            await RevokeAllForUserAsync(stored.UserId, now, cancellationToken);
            throw new AuthException("Refresh token invalide.");
        }

        if (!stored.IsActive(now))
            throw new AuthException("Refresh token expiré.");

        var user = await _userManager.FindByIdAsync(stored.UserId.ToString())
            ?? throw new AuthException("Refresh token invalide.");

        // Rotation : l'ancien token est révoqué, un nouveau est émis.
        // Les deux changements sont enregistrés ensemble dans IssueTokensAsync.
        stored.Revoke(now);
        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task LogoutAsync(
        RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var hash = _tokenService.HashToken(request.RefreshToken);

        var stored = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        // Token inconnu : rien à faire, la déconnexion reste sans erreur
        if (stored is null) return;

        stored.Revoke(DateTime.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthResponse> IssueTokensAsync(
        ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault()
            ?? throw new AuthException("Aucun rôle n'est associé à ce compte.");

        var access = _tokenService.CreateAccessToken(
            user.Id, user.Email!, user.FullName, role);
        var refresh = _tokenService.CreateRefreshToken();

        _db.RefreshTokens.Add(RefreshToken.Create(user.Id, refresh.Hash, refresh.ExpiresAt));
        await _db.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            access.Token,
            refresh.Token,
            access.ExpiresAt,
            user.Id,
            user.Email!,
            user.FullName,
            role);
    }

    private async Task RevokeAllForUserAsync(
        Guid userId, DateTime now, CancellationToken cancellationToken)
    {
        var active = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in active)
            token.Revoke(now);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
