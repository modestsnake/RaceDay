using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public record RegisterDto(
    [Required, StringLength(100)] string FullName,
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required] string Role,
    string? PhoneNumber,
    string? OrganiserCode);

public record LoginDto([Required] string Email, [Required] string Password);

public record ProfileUpdateDto([Required, StringLength(100)] string FullName, string? PhoneNumber);

public record EventDto(
    [Required, StringLength(150)] string EventName,
    DateOnly EventDate,
    [Required, StringLength(150)] string Location,
    string? Description,
    [Range(0.1, 1000)] decimal DistanceKm,
    [Required] string EventType);

public record CategoryDto(
    [Required, StringLength(100)] string CategoryName,
    [Range(0.1, 1000)] decimal DistanceKm,
    [Range(1, 100000)] int MaxParticipants = 100,
    [Range(0, 100000)] decimal EntryFee = 0);

public record EnrolDto(int CategoryId);

public record ResultDto(
    int EnrolmentId,
    TimeSpan? FinishTime,
    [Range(1, 1000000)] int? Position,
    [Required] string Status);