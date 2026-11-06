namespace Scholaris.Application.Common;

/// <summary>Ambient identity of the caller, resolved from the JWT by the API layer.</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Role { get; }
    bool IsInRole(string role);
}
