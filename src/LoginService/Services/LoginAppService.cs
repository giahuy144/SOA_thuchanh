using LoginService.Data;
using LoginService.Models;
using SOA.Shared.Contracts;

namespace LoginService.Services;

public interface ILoginAppService
{
    (bool Success, string? Error, AuthResponse? Response) Register(RegisterRequest request);
    (bool Success, string? Error, AuthResponse? Response) Login(LoginRequest request);
}

public sealed class LoginAppService : ILoginAppService
{
    private readonly IUserStore _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenIssuer _tokenIssuer;

    public LoginAppService(IUserStore users, IPasswordHasher passwordHasher, IJwtTokenIssuer tokenIssuer)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _tokenIssuer = tokenIssuer;
    }

    public (bool Success, string? Error, AuthResponse? Response) Register(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
        {
            return (false, "UserName và Password không được để trống.", null);
        }

        if (request.Password.Length < 6)
        {
            return (false, "Password phải có ít nhất 6 ký tự.", null);
        }

        if (_users.FindByUserName(request.UserName) is not null)
        {
            return (false, "Tên đăng nhập đã tồn tại.", null);
        }

        var user = new UserAccount
        {
            UserName = request.UserName.Trim(),
            FullName = string.IsNullOrWhiteSpace(request.FullName) ? request.UserName.Trim() : request.FullName.Trim(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = "User"
        };

        _users.Add(user);
        return (true, null, CreateResponse(user));
    }

    public (bool Success, string? Error, AuthResponse? Response) Login(LoginRequest request)
    {
        var user = _users.FindByUserName(request.UserName);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return (false, "Sai tên đăng nhập hoặc mật khẩu.", null);
        }

        return (true, null, CreateResponse(user));
    }

    private AuthResponse CreateResponse(UserAccount user)
    {
        var (token, expiresAt) = _tokenIssuer.Issue(user);
        return new AuthResponse(
            token,
            "Bearer",
            expiresAt,
            new UserProfile(user.Id, user.UserName, user.FullName, user.Role));
    }
}
