using Microsoft.EntityFrameworkCore;
using Scholaris.Application.Common;
using Scholaris.Application.Dtos;
using Scholaris.Application.Interfaces;
using Scholaris.Domain.Entities;
using Scholaris.Domain.Enums;
using Scholaris.Domain.Exceptions;

namespace Scholaris.Application.Services;

public class AuthService(IAppDbContext db, IPasswordHasher hasher, ITokenService tokens)
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest req)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(req.FullName) || req.FullName.Length < 2)
            throw new DomainException("Please provide your full name.");
        if (req.Password.Length < 6)
            throw new DomainException("Password must be at least 6 characters.");
        if (await db.Users.AnyAsync(u => u.Email == email))
            throw new DomainException("An account with this email already exists.");

        var user = new User
        {
            FullName = req.FullName.Trim(),
            Email = email,
            PasswordHash = hasher.Hash(req.Password),
            Role = UserRole.Student // public sign-up is always a student; staff accounts are seeded/managed
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return await IssueTokensAsync(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest req)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email)
                   ?? throw new DomainException("Invalid email or password.");
        if (!hasher.Verify(user.PasswordHash, req.Password))
            throw new DomainException("Invalid email or password.");
        return await IssueTokensAsync(user);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest req)
    {
        var rt = await db.RefreshTokens.Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Token == req.RefreshToken)
                ?? throw new DomainException("Invalid refresh token.");
        if (rt.RevokedAt != null || rt.ExpiresAt < DateTime.UtcNow)
            throw new DomainException("Refresh token expired. Please sign in again.");

        rt.RevokedAt = DateTime.UtcNow; // rotation
        return await IssueTokensAsync(rt.User);
    }

    private async Task<AuthResponse> IssueTokensAsync(User user)
    {
        var (access, _) = tokens.CreateAccessToken(user);
        var refresh = new RefreshToken
        {
            UserId = user.Id,
            Token = tokens.CreateRefreshToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        db.RefreshTokens.Add(refresh);
        await db.SaveChangesAsync();
        return new AuthResponse(access, refresh.Token,
            new UserDto(user.Id, user.FullName, user.Email, user.Role.ToString()));
    }
}
