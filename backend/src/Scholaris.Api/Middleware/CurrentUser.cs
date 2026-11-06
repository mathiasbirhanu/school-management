using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Scholaris.Application.Common;

namespace Scholaris.Api.Middleware;

/// <summary>Resolves the caller's identity from JWT claims attached by the auth middleware.</summary>
public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            var principal = accessor.HttpContext?.User;
            var value = principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Role => accessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role);

    public bool IsInRole(string role) => accessor.HttpContext?.User.IsInRole(role) ?? false;
}
