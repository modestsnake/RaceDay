namespace RaceDay.Api.Models;

public class User
{
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "Participant";
    public string? PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class Event
{
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = "";
    public string? Description { get; set; }
    public decimal DistanceKm { get; set; }
    public string EventType { get; set; } = "Run"; // Run, Walk, Cycle
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public User? CreatedBy { get; set; }
    public List<Category> Categories { get; set; } = new();
    public List<EventOrganiser> CoOrganisers { get; set; } = new();
}

public class EventOrganiser
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.Now;
    public Event? Event { get; set; }
    public User? Organiser { get; set; }
}

public class Category
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public int MaxParticipants { get; set; } = 100;
    public decimal EntryFee { get; set; }
    public Event? Event { get; set; }
}

public class Enrolment
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public int CategoryId { get; set; }
    public DateTime EnrolmentDate { get; set; } = DateTime.Now;
    public string BibNumber { get; set; } = "";
    public string Status { get; set; } = "Active";
    public User? Participant { get; set; }
    public Category? Category { get; set; }
    public Result? Result { get; set; }
}

public class Result
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = "Pending";
    public Enrolment? Enrolment { get; set; }
}