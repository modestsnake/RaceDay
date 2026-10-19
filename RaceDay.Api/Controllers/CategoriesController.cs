using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Authorize]
public class CategoriesController(RaceDayDbContext db) : ControllerBase
{
    /// <summary>List the categories of an event (both roles). Returns 200 or 404.</summary>
    [HttpGet("api/events/{eventId:int}/categories")]
    public async Task<IActionResult> ForEvent(int eventId)
    {
        if (!await db.Events.AnyAsync(e => e.EventId == eventId)) return NotFound();
        return Ok(await db.Categories.AsNoTracking().Where(c => c.EventId == eventId)
            .Select(c => new { c.CategoryId, c.EventId, c.CategoryName, c.DistanceKm, c.MaxParticipants, c.EntryFee })
            .ToListAsync());
    }

    /// <summary>Add a category to an event (Organiser who owns or co-organises it). Expects categoryName, distanceKm, maxParticipants, entryFee. Returns 201, 403 or 404.</summary>
    [HttpPost("api/events/{eventId:int}/categories")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Create(int eventId, CategoryDto dto)
    {
        if (!await db.Events.AnyAsync(e => e.EventId == eventId)) return NotFound();
        if (!await db.CanManageEvent(eventId, User.UserId())) return Forbid();
        var c = new Category
        {
            EventId = eventId, CategoryName = dto.CategoryName, DistanceKm = dto.DistanceKm,
            MaxParticipants = dto.MaxParticipants, EntryFee = dto.EntryFee
        };
        db.Categories.Add(c);
        await db.SaveChangesAsync();
        return Created($"/api/events/{eventId}/categories", new { c.CategoryId });
    }

    /// <summary>Update a category (Organiser who manages its event). Same body as create. Returns 204, 403 or 404.</summary>
    [HttpPut("api/categories/{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Update(int id, CategoryDto dto)
    {
        var c = await db.Categories.FindAsync(id);
        if (c is null) return NotFound();
        if (!await db.CanManageEvent(c.EventId, User.UserId())) return Forbid();
        c.CategoryName = dto.CategoryName; c.DistanceKm = dto.DistanceKm;
        c.MaxParticipants = dto.MaxParticipants; c.EntryFee = dto.EntryFee;
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Delete a category (Organiser who manages its event). Fails with 409 if anyone is enrolled. Returns 204, 403 or 404.</summary>
    [HttpDelete("api/categories/{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await db.Categories.FindAsync(id);
        if (c is null) return NotFound();
        if (!await db.CanManageEvent(c.EventId, User.UserId())) return Forbid();
        if (await db.Enrolments.AnyAsync(e => e.CategoryId == id))
            return Conflict("Category has enrolments and cannot be deleted.");
        db.Categories.Remove(c);
        await db.SaveChangesAsync();
        return NoContent();
    }
}