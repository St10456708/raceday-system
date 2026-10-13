using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDayApi.Models;

[Table("Users")]
public class AppUser
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("user_id")]
    public int UserId { get; set; }

    [Required, Column("full_name", TypeName = "varchar(100)")]
    public string FullName { get; set; } = string.Empty;

    [Required, Column("email", TypeName = "varchar(150)")]
    public string Email { get; set; } = string.Empty;

    [Required, Column("password_hash", TypeName = "varchar(255)")]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, Column("role", TypeName = "varchar(20)")]
    public string Role { get; set; } = string.Empty;

    [Column("phone_number", TypeName = "varchar(20)")]
    public string? PhoneNumber { get; set; }

    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

[Table("Events")]
public class RaceEvent
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("event_id")]
    public int EventId { get; set; }

    [Column("organiser_id")]
    public int OrganiserId { get; set; }

    [Required, Column("event_name", TypeName = "varchar(150)")]
    public string EventName { get; set; } = string.Empty;

    [Column("description", TypeName = "varchar(1000)")]
    public string? Description { get; set; }

    [Column("event_date", TypeName = "date")]
    public DateOnly EventDate { get; set; }

    [Required, Column("event_type", TypeName = "varchar(20)")]
    public string EventType { get; set; } = string.Empty;

    [Required, Column("status", TypeName = "varchar(20)")]
    public string Status { get; set; } = "Upcoming";

    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey(nameof(OrganiserId))]
    public AppUser Organiser { get; set; } = null!;

    public Venue? Venue { get; set; }

    public List<Category> Categories { get; set; } = new();
}

[Table("Venues")]
public class Venue
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("venue_id")]
    public int VenueId { get; set; }

    [Column("event_id")]
    public int EventId { get; set; }

    [Required, Column("venue_name", TypeName = "varchar(150)")]
    public string VenueName { get; set; } = string.Empty;

    [Required, Column("address", TypeName = "varchar(255)")]
    public string Address { get; set; } = string.Empty;

    [Required, Column("city", TypeName = "varchar(100)")]
    public string City { get; set; } = string.Empty;

    [Column("latitude", TypeName = "decimal(9,6)")]
    public decimal? Latitude { get; set; }

    [Column("longitude", TypeName = "decimal(9,6)")]
    public decimal? Longitude { get; set; }

    [ForeignKey(nameof(EventId))]
    public RaceEvent Event { get; set; } = null!;
}

[Table("Categories")]
public class Category
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("category_id")]
    public int CategoryId { get; set; }

    [Column("event_id")]
    public int EventId { get; set; }

    [Required, Column("category_name", TypeName = "varchar(100)")]
    public string CategoryName { get; set; } = string.Empty;

    [Column("distance_km", TypeName = "decimal(5,2)")]
    public decimal DistanceKm { get; set; }

    [Column("price", TypeName = "decimal(8,2)")]
    public decimal Price { get; set; }

    [Column("max_participants")]
    public int MaxParticipants { get; set; } = 100;

    [ForeignKey(nameof(EventId))]
    public RaceEvent Event { get; set; } = null!;

    public List<Enrolment> Enrolments { get; set; } = new();
}

[Table("Enrolments")]
public class Enrolment
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("enrolment_id")]
    public int EnrolmentId { get; set; }

    [Column("participant_id")]
    public int ParticipantId { get; set; }

    [Column("category_id")]
    public int CategoryId { get; set; }

    [Column("enrolment_date", TypeName = "datetime")]
    public DateTime EnrolmentDate { get; set; } = DateTime.Now;

    [Column("bib_number", TypeName = "varchar(10)")]
    public string? BibNumber { get; set; }

    [Required, Column("status", TypeName = "varchar(20)")]
    public string Status { get; set; } = "Confirmed";

    [ForeignKey(nameof(ParticipantId))]
    public AppUser Participant { get; set; } = null!;

    [ForeignKey(nameof(CategoryId))]
    public Category Category { get; set; } = null!;

    public RaceResult? Result { get; set; }
}

[Table("Results")]
public class RaceResult
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("result_id")]
    public int ResultId { get; set; }

    [Column("enrolment_id")]
    public int EnrolmentId { get; set; }

    [Column("finish_time", TypeName = "time")]
    public TimeSpan? FinishTime { get; set; }

    [Column("position")]
    public int? Position { get; set; }

    [Required, Column("status", TypeName = "varchar(20)")]
    public string Status { get; set; } = "Finished";

    [Column("recorded_at", TypeName = "datetime")]
    public DateTime RecordedAt { get; set; } = DateTime.Now;

    [ForeignKey(nameof(EnrolmentId))]
    public Enrolment Enrolment { get; set; } = null!;
}