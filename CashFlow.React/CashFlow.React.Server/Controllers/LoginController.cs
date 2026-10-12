using CashFlow.DataAccess.Repositories;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace CashFlow.React.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class LoginController : ControllerBase
{
    private readonly UserEntityRepository userRepo;

    public LoginController(UserEntityRepository userRepo)
    {
        this.userRepo = userRepo;
    }

    [HttpGet("Antiforgery")]
    [AllowAnonymous]
    public IActionResult Antiforgery([FromServices] IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { token = tokens.RequestToken });
    }

    [HttpPost("Login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login([FromBody] LoginRequest model)
    {
        var user = userRepo.Authentication(model.UserName.Trim(), model.Password);
        if (user is null)
            return Unauthorized(new { message = "Invalid username or password." });

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.OID.ToString()),
            new Claim(ClaimTypes.Name, user.UserName)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = false });

        return Ok(new { userName = user.UserName });
    }

    [HttpGet("Me")]
    public IActionResult Me() => Ok(new { userName = User.Identity!.Name });


    [HttpPost("LogOff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogOff()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    // Uses the same JSON fields as your existing LoginViewModel.
    // HasPassword is not required for a sign-in request.
    public sealed class LoginRequest
    {
        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
