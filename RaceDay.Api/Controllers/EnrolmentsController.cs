using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Authorize]
public class EnrolmentsController(RaceDayDbContext db) : ControllerBase
{
    /// <summary>Enter an event (Participant only). Expects categoryId. Records the participant, event category and a generated bib number. Returns 201, 404 (no such category) or 409 (already enrolled or category full).</summary>
    [HttpPost("api/enrolments")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> Enrol(EnrolDto dto)
    {
        var cat = await db.Categories.FindAsync(dto.CategoryId);
        if (cat is null) return NotFound("Category not found.");
        var uid = User.UserId();
        if (await db.Enrolments.AnyAsync(e => e.ParticipantId == uid && e.CategoryId == cat.CategoryId))
            return Conflict("Already enrolled in this category.");
        var active = await db.Enrolments.CountAsync(e => e.CategoryId == cat.CategoryId && e.Status == "Active");
        if (active >= cat.MaxParticipants) return Conflict("Category is full.");

        var en = new Enrolment
        {
            ParticipantId = uid,
            CategoryId = cat.CategoryId,
            BibNumber = Guid.NewGuid().ToString("N")[..10]
        };
        db.Enrolments.Add(en);
        await db.SaveChangesAsync();
        en.BibNumber = $"BIB-{1000 + en.EnrolmentId}";
        await db.SaveChangesAsync();
        return Created($"/api/enrolments/{en.EnrolmentId}", new { en.EnrolmentId, en.CategoryId, en.BibNumber, en.Status });
    }

    /// <summary>List the logged-in Participant's own enrolments. Returns 200 with a list.</summary>
    [HttpGet("api/enrolments/my")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> Mine()
    {
        var uid = User.UserId();
        return Ok(await db.Enrolments.AsNoTracking().Where(e => e.ParticipantId == uid)
            .Select(e => new
            {
                e.EnrolmentId, e.BibNumber, e.Status, e.EnrolmentDate, e.CategoryId,
                CategoryName = e.Category!.CategoryName,
                EventId = e.Category.EventId,
                EventName = e.Category.Event!.EventName,
                EventDate = e.Category.Event.EventDate
            }).ToListAsync());
    }

    /// <summary>List all enrolments for an event (Organiser who manages it). Returns 200, 403 or 404.</summary>
    [HttpGet("api/events/{eventId:int}/enrolments")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> ForEvent(int eventId)
    {
        if (!await db.Events.AnyAsync(e => e.EventId == eventId)) return NotFound();
        if (!await db.CanManageEvent(eventId, User.UserId())) return Forbid();
        return Ok(await db.Enrolments.AsNoTracking().Where(e => e.Category!.EventId == eventId)
            .Select(e => new
            {
                e.EnrolmentId, e.BibNumber, e.Status, e.EnrolmentDate, e.ParticipantId,
                ParticipantName = e.Participant!.FullName,
                e.CategoryId,
                CategoryName = e.Category!.CategoryName
            }).ToListAsync());
    }
}