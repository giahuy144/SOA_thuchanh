namespace SOA.Shared.Contracts;

public sealed record RegisterRequest(string UserName, string Password, string? FullName);

public sealed record LoginRequest(string UserName, string Password);

public sealed record AuthResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAtUtc,
    UserProfile User);

public sealed record UserProfile(Guid Id, string UserName, string FullName, string Role);

public sealed record ApiMessage(string Message);
