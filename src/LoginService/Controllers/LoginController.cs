using LoginService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOA.Shared.Contracts;

namespace LoginService.Controllers;

[ApiController]
[Route("api/login")]
public sealed class LoginController : ControllerBase
{
    private readonly ILoginAppService _loginService;

    public LoginController(ILoginAppService loginService) => _loginService = loginService;

    [HttpPost("register")]
    [AllowAnonymous]
    public ActionResult<AuthResponse> Register([FromBody] RegisterRequest request)
    {
        var result = _loginService.Register(request);
        return result.Success ? Ok(result.Response) : BadRequest(new ApiMessage(result.Error!));
    }

    [HttpPost]
    [AllowAnonymous]
    public ActionResult<AuthResponse> Login([FromBody] LoginRequest request)
    {
        var result = _loginService.Login(request);
        return result.Success ? Ok(result.Response) : Unauthorized(new ApiMessage(result.Error!));
    }

    [HttpGet("me")]
    [Authorize]
    public ActionResult<UserProfile> Me()
    {
        return Ok(new UserProfile(
            Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value),
            User.Identity?.Name ?? string.Empty,
            User.FindFirst("fullName")?.Value ?? string.Empty,
            User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "User"));
    }
}
