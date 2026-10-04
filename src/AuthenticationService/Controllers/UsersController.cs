using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SOA.Shared.Contracts;

namespace AuthenticationService.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    [HttpGet("me")]
    public ActionResult<UserProfile> Me()
    {
        return Ok(ToProfile(User));
    }

    [HttpGet]
    public IActionResult Directory()
    {
        if (!User.IsInRole("Admin"))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiMessage("Chỉ Admin mới xem được danh sách người dùng."));
        }

        return Ok(new
        {
            message = "Endpoint bảo vệ bằng JWT + kiểm tra Role.",
            caller = ToProfile(User)
        });
    }

    private static UserProfile ToProfile(ClaimsPrincipal user) => new(
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString()),
        user.Identity?.Name ?? string.Empty,
        user.FindFirstValue("fullName") ?? string.Empty,
        user.FindFirstValue(ClaimTypes.Role) ?? "User");
}
