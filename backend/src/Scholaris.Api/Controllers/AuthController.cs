using Microsoft.AspNetCore.Mvc;
using Scholaris.Application.Dtos;
using Scholaris.Application.Services;

namespace Scholaris.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService auth) : ControllerBase
{
    /// <summary>Public sign-up — always creates a Student account.</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest req) => await auth.RegisterAsync(req);

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req) => await auth.LoginAsync(req);

    /// <summary>Exchanges a refresh token for a new pair (rotation: old token is revoked).</summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest req) => await auth.RefreshAsync(req);
}
