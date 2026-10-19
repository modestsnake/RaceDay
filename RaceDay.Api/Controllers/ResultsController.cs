using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Authorize]
public class ResultsController(RaceDayDbContext db) : ControllerBase
{
    private static readonly string[] Statuses = ["Pending", "Finished", "DNF", "DSQ"];

    /// <summary>Capture or update a result (Organiser who manages the event). Expects enrolmentId, finishTime (hh:mm:ss), position, status (Pending, Finished, DNF or DSQ). Returns 200, 400, 403 or 404.</summary>
    [HttpPut("api/results")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Save(ResultDto dto)
    {
        if (!Statuses.Contains(dto.Status)) return BadRequest("Status must be Pending, Finished, DNF or DSQ.");
        var en = await db.Enrolments.Include(e => e.Category).Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.EnrolmentId == dto.EnrolmentId);
        if (en is null) return NotFound("Enrolment not found.");
        if (!await db.CanManageEvent(en.Category!.EventId, User.UserId())) return Forbid();

        var r = en.Result ?? new Result { EnrolmentId = en.EnrolmentId };
        if (en.Result is null) db.Results.Add(r);
        r.FinishTime = dto.FinishTime;
        r.Position = dto.Position;
        r.Status = dto.Status;
        await db.SaveChangesAsync();
        return Ok(new { r.ResultId, r.EnrolmentId, r.FinishTime, r.Position, r.Status });
    }

    /// <summary>List the logged-in Participant's own results. Returns 200 with a list.</summary>
    [HttpGet("api/results/my")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> Mine()
    {
        var uid = User.UserId();
        return Ok(await db.Results.AsNoTracking().Where(r => r.Enrolment!.ParticipantId == uid)
            .Select(r => new
            {
                r.ResultId, r.EnrolmentId,
                BibNumber = r.Enrolment!.BibNumber,
                EventName = r.Enrolment.Category!.Event!.EventName,
                CategoryName = r.Enrolment.Category.CategoryName,
                r.FinishTime, r.Position, r.Status
            }).ToListAsync());
    }

    /// <summary>List all results for an event (Organiser who manages it). Returns 200, 403 or 404.</summary>
    [HttpGet("api/events/{eventId:int}/results")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> ForEvent(int eventId)
    {
        if (!await db.Events.AnyAsync(e => e.EventId == eventId)) return NotFound();
        if (!await db.CanManageEvent(eventId, User.UserId())) return Forbid();
        return Ok(await db.Results.AsNoTracking().Where(r => r.Enrolment!.Category!.EventId == eventId)
            .Select(r => new
            {
                r.ResultId, r.EnrolmentId,
                BibNumber = r.Enrolment!.BibNumber,
                ParticipantName = r.Enrolment.Participant!.FullName,
                r.FinishTime, r.Position, r.Status
            }).ToListAsync());
    }
}