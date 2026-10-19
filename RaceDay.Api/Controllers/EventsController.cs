using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
[Authorize]
public class EventsController(RaceDayDbContext db) : ControllerBase
{
    private static readonly string[] Types = ["Run", "Walk", "Cycle"];

    /// <summary>List events (both roles). Optional query upcoming=true returns only future events. Returns 200 with a list.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool upcoming = false)
    {
        var q = db.Events.AsNoTracking();
        if (upcoming)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            q = q.Where(e => e.EventDate >= today);
        }
        return Ok(await q.OrderBy(e => e.EventDate)
            .Select(e => new { e.EventId, e.EventName, e.EventDate, e.Location, e.Description, e.DistanceKm, e.EventType, e.CreatedByUserId })
            .ToListAsync());
    }

    /// <summary>Get one event by id (both roles). Returns 200 or 404.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var e = await db.Events.AsNoTracking().Where(x => x.EventId == id)
            .Select(x => new { x.EventId, x.EventName, x.EventDate, x.Location, x.Description, x.DistanceKm, x.EventType, x.CreatedByUserId })
            .FirstOrDefaultAsync();
        return e is null ? NotFound() : Ok(e);
    }

    /// <summary>Create an event (Organiser only). Expects eventName, eventDate (yyyy-MM-dd), location, description, distanceKm, eventType (Run, Walk or Cycle). Returns 201 with the new eventId.</summary>
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Create(EventDto dto)
    {
        if (!Types.Contains(dto.EventType)) return BadRequest("EventType must be Run, Walk or Cycle.");
        var ev = new Event
        {
            EventName = dto.EventName, EventDate = dto.EventDate, Location = dto.Location,
            Description = dto.Description, DistanceKm = dto.DistanceKm, EventType = dto.EventType,
            CreatedByUserId = User.UserId()
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        return Created($"/api/events/{ev.EventId}", new { ev.EventId });
    }

    /// <summary>Update an event (Organiser who owns or co-organises it). Same body as create. Returns 204, 403 or 404.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Update(int id, EventDto dto)
    {
        if (!Types.Contains(dto.EventType)) return BadRequest("EventType must be Run, Walk or Cycle.");
        var ev = await db.Events.FindAsync(id);
        if (ev is null) return NotFound();
        if (!await db.CanManageEvent(id, User.UserId())) return Forbid();
        ev.EventName = dto.EventName; ev.EventDate = dto.EventDate; ev.Location = dto.Location;
        ev.Description = dto.Description; ev.DistanceKm = dto.DistanceKm; ev.EventType = dto.EventType;
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Delete an event (Organiser who owns or co-organises it). Fails with 409 if the event still has categories. Returns 204, 403 or 404.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await db.Events.FindAsync(id);
        if (ev is null) return NotFound();
        if (!await db.CanManageEvent(id, User.UserId())) return Forbid();
        if (await db.Categories.AnyAsync(c => c.EventId == id))
            return Conflict("Delete the event's categories first.");
        db.EventOrganisers.RemoveRange(db.EventOrganisers.Where(o => o.EventId == id));
        db.Events.Remove(ev);
        await db.SaveChangesAsync();
        return NoContent();
    }
}