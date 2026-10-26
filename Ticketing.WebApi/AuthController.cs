using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticketing.Application.Common.Interfaces;
using Ticketing.Application.DTOs.Auth;

namespace Ticketing.WebApi.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RefreshTokenRequest> _refreshValidator;

    public AuthController(
        IAuthService authService,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<RefreshTokenRequest> refreshValidator)
    {
        _authService = authService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _refreshValidator = refreshValidator;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(
        RegisterRequest request, CancellationToken cancellationToken)
    {
        await _registerValidator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await _authService.RegisterAsync(request, cancellationToken));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request, CancellationToken cancellationToken)
    {
        await _loginValidator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await _authService.LoginAsync(request, cancellationToken));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh(
        RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await _refreshValidator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await _authService.RefreshAsync(request, cancellationToken));
    }

    // Public : l'access token peut déjà être expiré au moment de la déconnexion
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(
        RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await _refreshValidator.ValidateAndThrowAsync(request, cancellationToken);
        await _authService.LogoutAsync(request, cancellationToken);
        return NoContent();
    }

    // Sert à vérifier que le JWT et le rôle sont bien lus
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me() => Ok(new
    {
        Id = User.FindFirstValue(ClaimTypes.NameIdentifier),
        Email = User.FindFirstValue(ClaimTypes.Email),
        Name = User.FindFirstValue(ClaimTypes.Name),
        Role = User.FindFirstValue(ClaimTypes.Role)
    });
}