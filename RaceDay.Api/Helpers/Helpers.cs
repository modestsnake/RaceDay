using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;

namespace RaceDay.Api;

public static class Helpers
{
    public static int UserId(this ClaimsPrincipal p) =>
        int.Parse(p.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public static Task<bool> CanManageEvent(this RaceDayDbContext db, int eventId, int userId) =>
        db.Events.AnyAsync(e => e.EventId == eventId &&
            (e.CreatedByUserId == userId || e.CoOrganisers.Any(c => c.OrganiserId == userId)));
}
