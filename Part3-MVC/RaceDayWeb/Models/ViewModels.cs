using System.ComponentModel.DataAnnotations;

namespace RaceDayWeb.Models;

public class RegisterVm
{
    [Required, StringLength(100), Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = "";

    [Required, StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters."), DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required, DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "Passwords do not match."), Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = "";

    [Required]
    public string Role { get; set; } = "Participant";

    [Phone, StringLength(20), Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }
}

public class LoginVm
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

public class ProfileVm
{
    [Required, StringLength(100), Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [Phone, StringLength(20), Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public string? ProfilePictureUrl { get; set; }
}

public class EventFormVm
{
    public int EventId { get; set; }
    public bool IsEdit { get; set; }

    [Required, StringLength(150), Display(Name = "Event name")]
    public string EventName { get; set; } = "";

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required, DataType(DataType.Date), Display(Name = "Event date")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime? EventDate { get; set; } = DateTime.Today.AddDays(30);

    [Required, Display(Name = "Event type")]
    public string EventType { get; set; } = "Run";

    public string Status { get; set; } = "Upcoming";

    [Required, StringLength(150), Display(Name = "Venue name")]
    public string VenueName { get; set; } = "";

    [Required, StringLength(255), Display(Name = "Address")]
    public string Address { get; set; } = "";

    [Required, StringLength(100), Display(Name = "City")]
    public string City { get; set; } = "";

    [Range(0.01, 999.99, ErrorMessage = "Distance must be between 0.01 and 999.99 km."), Display(Name = "Main distance (km)")]
    public decimal DistanceKm { get; set; } = 5;

    [Range(0, 999999, ErrorMessage = "Enter a valid fee."), Display(Name = "Entry fee (R)")]
    public decimal Price { get; set; } = 100;

    [Range(1, 100000, ErrorMessage = "Enter a number of participants."), Display(Name = "Max participants")]
    public int MaxParticipants { get; set; } = 100;

    [Display(Name = "Banner image")]
    public IFormFile? Banner { get; set; }

    public string? BannerUrl { get; set; }
}

public class CategoryVm
{
    public int EventId { get; set; }

    [Required, StringLength(100), Display(Name = "Category name")]
    public string CategoryName { get; set; } = "";

    [Range(0.01, 999.99, ErrorMessage = "Distance must be between 0.01 and 999.99 km."), Display(Name = "Distance (km)")]
    public decimal DistanceKm { get; set; } = 10;

    [Range(0, 999999, ErrorMessage = "Enter a valid fee."), Display(Name = "Price (R)")]
    public decimal Price { get; set; } = 100;

    [Range(1, 100000), Display(Name = "Max participants")]
    public int MaxParticipants { get; set; } = 100;
}

public class ResultVm
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }

    [Required]
    public string Status { get; set; } = "Finished";

    [RegularExpression(@"^\d{2}:\d{2}:\d{2}$", ErrorMessage = "Finish time must look like 00:52:14.")]
    public string? FinishTime { get; set; }

    [Range(1, 100000)]
    public int? Position { get; set; }
}