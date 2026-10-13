using System.ComponentModel.DataAnnotations;
using RaceDayApi.Models;

namespace RaceDayApi.Dtos;

// =====================  REQUESTS (data the client sends)  =====================

/// <summary>Data needed to register a new account.</summary>
public class RegisterRequest
{
    /// <example>Lindiwe Zulu</example>
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    /// <example>lindiwe@example.com</example>
    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    /// <example>Passw0rd!</example>
    [Required, MinLength(6), MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    /// <summary>Must be exactly "Organiser" or "Participant".</summary>
    /// <example>Participant</example>
    [Required]
    public string Role { get; set; } = string.Empty;

    /// <example>0821234567</example>
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
}

/// <summary>Data needed to log in.</summary>
public class LoginRequest
{
    /// <example>lindiwe@example.com</example>
    [Required]
    public string Email { get; set; } = string.Empty;

    /// <example>Passw0rd!</example>
    [Required]
    public string Password { get; set; } = string.Empty;
}

/// <summary>Profile fields a user may change.</summary>
public class UpdateProfileRequest
{
    /// <example>Lindiwe Zulu-Smith</example>
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    /// <example>0725551234</example>
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
}

/// <summary>Venue (location) of an event.</summary>
public class VenueRequest
{
    /// <example>Union Buildings Gardens</example>
    [Required, MaxLength(150)]
    public string VenueName { get; set; } = string.Empty;

    /// <example>1 Government Ave, Arcadia</example>
    [Required, MaxLength(255)]
    public string Address { get; set; } = string.Empty;

    /// <example>Pretoria</example>
    [Required, MaxLength(100)]
    public string City { get; set; } = string.Empty;

    /// <example>-25.744186</example>
    public decimal? Latitude { get; set; }

    /// <example>28.211477</example>
    public decimal? Longitude { get; set; }
}

/// <summary>Data needed to create an event.</summary>
public class CreateEventRequest
{
    /// <example>Pretoria Park Run Challenge</example>
    [Required, MaxLength(150)]
    public string EventName { get; set; } = string.Empty;

    /// <example>A community run through Pretoria city parks.</example>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>Format: yyyy-MM-dd</summary>
    /// <example>2026-11-14</example>
    [Required]
    public DateOnly? EventDate { get; set; }

    /// <summary>Must be "Run", "Walk" or "Cycle".</summary>
    /// <example>Run</example>
    [Required]
    public string EventType { get; set; } = string.Empty;

    [Required]
    public VenueRequest Venue { get; set; } = new();
}

/// <summary>Data needed to update an event.</summary>
public class UpdateEventRequest
{
    /// <example>Pretoria Park Run Challenge</example>
    [Required, MaxLength(150)]
    public string EventName { get; set; } = string.Empty;

    /// <example>Updated description.</example>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>Format: yyyy-MM-dd</summary>
    /// <example>2026-11-15</example>
    [Required]
    public DateOnly? EventDate { get; set; }

    /// <summary>Must be "Run", "Walk" or "Cycle".</summary>
    /// <example>Run</example>
    [Required]
    public string EventType { get; set; } = string.Empty;

    /// <summary>Must be "Upcoming", "Completed" or "Cancelled".</summary>
    /// <example>Upcoming</example>
    [Required]
    public string Status { get; set; } = string.Empty;
}

/// <summary>Data needed to create or update a race category.</summary>
public class CategoryRequest
{
    /// <example>10km Run</example>
    [Required, MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    /// <example>10</example>
    [Range(0.01, 999.99)]
    public decimal DistanceKm { get; set; }

    /// <example>150</example>
    [Range(0, 999999.99)]
    public decimal Price { get; set; }

    /// <example>300</example>
    [Range(1, 100000)]
    public int MaxParticipants { get; set; }
}

/// <summary>Data needed to capture a result.</summary>
public class ResultRequest
{
    /// <summary>Format: hh:mm:ss</summary>
    /// <example>00:52:14</example>
    public string? FinishTime { get; set; }

    /// <example>5</example>
    [Range(1, 100000)]
    public int? Position { get; set; }

    /// <summary>Must be "Finished", "DNF" or "DNS".</summary>
    /// <example>Finished</example>
    [Required]
    public string Status { get; set; } = string.Empty;
}

// =====================  RESPONSES (data the API returns)  =====================

public record MessageResponse(string Message);

public record AuthResponse(int UserId, string FullName, string Email, string Role, string Message);

public record UserResponse(int UserId, string FullName, string Email, string Role, string? PhoneNumber);

public record VenueResponse(int VenueId, string VenueName, string Address, string City, decimal? Latitude, decimal? Longitude);

public record CategoryResponse(int CategoryId, int EventId, string CategoryName, decimal DistanceKm, decimal Price, int MaxParticipants);

public record EventResponse(int EventId, int OrganiserId, string EventName, string? Description, DateOnly EventDate,
    string EventType, string Status, VenueResponse? Venue, List<CategoryResponse> Categories);

public record EnrolmentResponse(int EnrolmentId, int ParticipantId, string ParticipantName, int CategoryId, string CategoryName,
    int EventId, string EventName, string? BibNumber, string Status, DateTime EnrolmentDate);

public record ResultResponse(int ResultId, int EnrolmentId, int EventId, string EventName, int ParticipantId, string ParticipantName,
    string CategoryName, string? BibNumber, string? FinishTime, int? Position, string Status);

// =====================  MAPPING (entity -> response)  =====================

public static class Mapper
{
    public static CategoryResponse ToResponse(this Category c) =>
        new(c.CategoryId, c.EventId, c.CategoryName, c.DistanceKm, c.Price, c.MaxParticipants);

    public static EventResponse ToResponse(this RaceEvent e) =>
        new(e.EventId, e.OrganiserId, e.EventName, e.Description, e.EventDate, e.EventType, e.Status,
            e.Venue == null
                ? null
                : new VenueResponse(e.Venue.VenueId, e.Venue.VenueName, e.Venue.Address, e.Venue.City, e.Venue.Latitude, e.Venue.Longitude),
            e.Categories.Select(c => c.ToResponse()).ToList());

    // Needs Participant, Category and Category.Event to be loaded.
    public static EnrolmentResponse ToResponse(this Enrolment e) =>
        new(e.EnrolmentId, e.ParticipantId, e.Participant.FullName, e.CategoryId, e.Category.CategoryName,
            e.Category.EventId, e.Category.Event.EventName, e.BibNumber, e.Status, e.EnrolmentDate);

    // Needs Enrolment, Enrolment.Participant, Enrolment.Category and Enrolment.Category.Event to be loaded.
    public static ResultResponse ToResponse(this RaceResult r) =>
        new(r.ResultId, r.EnrolmentId, r.Enrolment.Category.EventId, r.Enrolment.Category.Event.EventName,
            r.Enrolment.ParticipantId, r.Enrolment.Participant.FullName, r.Enrolment.Category.CategoryName,
            r.Enrolment.BibNumber,
            r.FinishTime?.ToString(@"hh\:mm\:ss"), r.Position, r.Status);
}