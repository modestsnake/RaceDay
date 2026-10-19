using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(RaceDayDbContext db, IConfiguration config) : ControllerBase
{
    private readonly PasswordHasher<User> _hasher = new();

    /// <summary>Register a new account. Expects fullName, email, password (8+ chars), role (Organiser or Participant), optional phoneNumber. Organisers must also supply the organiserCode. Returns 201 with the new user, 400 for bad input, 403 for a wrong organiser code, 409 if the email exists.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (dto.Role is not ("Organiser" or "Participant"))
            return BadRequest("Role must be Organiser or Participant.");
        if (dto.Role == "Organiser" && dto.OrganiserCode != config["OrganiserCode"])
            return StatusCode(403, "Invalid organiser access code.");

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email))
            return Conflict("Email already registered.");

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = email,
            Role = dto.Role,
            PhoneNumber = dto.PhoneNumber
        };
        user.PasswordHash = _hasher.HashPassword(user, dto.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return Created("/api/profile", new { user.UserId, user.FullName, user.Email, user.Role });
    }

    /// <summary>Log in. Expects email and password. Starts a cookie session holding the user's id and role. Returns 200 with user details, or 401 for invalid credentials.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null ||
            _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password) == PasswordVerificationResult.Failed)
            return Unauthorized("Invalid email or password.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        return Ok(new { user.UserId, user.FullName, user.Role });
    }

    /// <summary>Log out and end the session. Requires login. Returns 200.</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok("Logged out.");
    }
}