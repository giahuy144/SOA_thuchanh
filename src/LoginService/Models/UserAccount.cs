namespace LoginService.Models;

public sealed class UserAccount
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string UserName { get; init; }
    public required string FullName { get; set; }
    public required string PasswordHash { get; set; }
    public required string Role { get; set; }
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}
