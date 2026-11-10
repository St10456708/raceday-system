namespace RaceDayWeb.Models;

public record AuthResponse(int UserId, string FullName, string Email, string Role, string Message);
public record UserDto(int UserId, string FullName, string Email, string Role, string? PhoneNumber, string? ProfilePictureUrl);
public record VenueDto(int VenueId, string VenueName, string Address, string City, decimal? Latitude, decimal? Longitude);
public record CategoryDto(int CategoryId, int EventId, string CategoryName, decimal DistanceKm, decimal Price, int MaxParticipants);
public record EventDto(int EventId, int OrganiserId, string EventName, string? Description, DateOnly EventDate,
    string EventType, string Status, VenueDto? Venue, List<CategoryDto> Categories, string? BannerUrl);
public record EnrolmentDto(int EnrolmentId, int ParticipantId, string ParticipantName, int CategoryId, string CategoryName,
    int EventId, string EventName, string? BibNumber, string Status, DateTime EnrolmentDate);
public record ResultDto(int ResultId, int EnrolmentId, int EventId, string EventName, int ParticipantId, string ParticipantName,
    string CategoryName, string? BibNumber, string? FinishTime, int? Position, string Status);

// page models
public record DashboardRow(EventDto Event, int Confirmed, int Total);
public record ResultRow(ResultDto Result, DateOnly? EventDate, int Finishers);
public record CategoriesPage(EventDto Event, CategoryVm Form);
public record EnrolmentsPage(EventDto Event, List<EnrolmentDto> Items);
public record ResultsPage(EventDto Event, List<EnrolmentDto> Enrolments, Dictionary<int, ResultDto> Existing);