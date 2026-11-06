using Scholaris.Domain.Entities;

namespace Scholaris.Application.Interfaces;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string hash, string password);
}

public interface ITokenService
{
    /// <summary>Creates a short-lived signed JWT carrying sub/role/email claims.</summary>
    (string Token, DateTime ExpiresAt) CreateAccessToken(User user);
    /// <summary>Creates an opaque refresh token (rotated on every use).</summary>
    string CreateRefreshToken();
}
