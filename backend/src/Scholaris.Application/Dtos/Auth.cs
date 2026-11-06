namespace Scholaris.Application.Dtos;

public record RegisterRequest(string FullName, string Email, string Password);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
public record UserDto(Guid Id, string FullName, string Email, string Role);
public record AuthResponse(string AccessToken, string RefreshToken, UserDto User);
