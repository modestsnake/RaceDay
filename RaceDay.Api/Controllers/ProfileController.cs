using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController(RaceDayDbContext db) : ControllerBase
{
    /// <summary>Get the logged-in user's own profile. No input. Returns 200 with id, name, email, role, phone.</summary>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var id = User.UserId();
        var u = await db.Users.AsNoTracking().Where(x => x.UserId == id)
            .Select(x => new { x.UserId, x.FullName, x.Email, x.Role, x.PhoneNumber, x.CreatedAt })
            .FirstOrDefaultAsync();
        return u is null ? NotFound() : Ok(u);
    }

    /// <summary>Update the logged-in user's own name and phone number. Expects fullName and optional phoneNumber. Returns 204.</summary>
    [HttpPut]
    public async Task<IActionResult> Update(ProfileUpdateDto dto)
    {
        var u = await db.Users.FindAsync(User.UserId());
        if (u is null) return NotFound();
        u.FullName = dto.FullName.Trim();
        u.PhoneNumber = dto.PhoneNumber;
        await db.SaveChangesAsync();
        return NoContent();
    }
}