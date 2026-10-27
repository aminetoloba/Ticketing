using System.Security.Claims;
using Ticketing.Application.Common.Interfaces;

namespace Ticketing.WebApi.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId =>
        Guid.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string? Role => User?.FindFirstValue(ClaimTypes.Role);

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserIdentifier
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.FindFirstValue(ClaimTypes.Email)
                ?? user?.FindFirstValue(ClaimTypes.NameIdentifier);
        }
    }
}