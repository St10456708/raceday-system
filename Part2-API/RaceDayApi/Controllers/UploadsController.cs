using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.Dtos;
using RaceDayApi.Services;

namespace RaceDayApi.Controllers;

[Tags("Image Uploads")]
public class UploadsController : ApiControllerBase
{
    private readonly AppDbContext _db;
    private readonly IBlobStorageService _blob;

    public UploadsController(AppDbContext db, IBlobStorageService blob)
    {
        _db = db;
        _blob = blob;
    }

    /// <summary>Upload an event banner image to Azure Blob Storage (owning Organiser only).</summary>
    [HttpPost("api/events/{id:int}/banner")]
    [SessionAuthorize("Organiser")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadBanner(int id, IFormFile file)
    {
        var ev = await _db.Events.Include(e => e.Venue).Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventId == id);
        if (ev == null)
        {
            return NotFound(new MessageResponse("Event not found."));
        }

        if (ev.OrganiserId != CurrentUserId!.Value)
        {
            return ForbiddenResult("You can only change your own events.");
        }

        try
        {
            ev.BannerUrl = await _blob.UploadImageAsync(file, "banners");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new MessageResponse(ex.Message));
        }
        catch (Exception)
        {
            return StatusCode(503, new MessageResponse("Image storage is unavailable right now."));
        }

        await _db.SaveChangesAsync();
        return Ok(ev.ToResponse());
    }

    /// <summary>Upload my profile picture to Azure Blob Storage.</summary>
    [HttpPost("api/users/me/picture")]
    [SessionAuthorize]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadPicture(IFormFile file)
    {
        var user = await _db.Users.FindAsync(CurrentUserId!.Value);
        if (user == null)
        {
            return Unauthorized(new MessageResponse("Your account no longer exists."));
        }

        try
        {
            user.ProfilePictureUrl = await _blob.UploadImageAsync(file, "profiles");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new MessageResponse(ex.Message));
        }
        catch (Exception)
        {
            return StatusCode(503, new MessageResponse("Image storage is unavailable right now."));
        }

        await _db.SaveChangesAsync();
        return Ok(new UserResponse(user.UserId, user.FullName, user.Email, user.Role, user.PhoneNumber, user.ProfilePictureUrl));
    }
}

[Tags("Events")]
public class MyEventsController : ApiControllerBase
{
    private readonly AppDbContext _db;

    public MyEventsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>List ALL events I created, in any status (Organiser only).</summary>
    [HttpGet("api/events/mine")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(typeof(List<EventResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine()
    {
        var userId = CurrentUserId!.Value;
        var events = await _db.Events
            .Include(e => e.Venue)
            .Include(e => e.Categories)
            .Where(e => e.OrganiserId == userId)
            .OrderBy(e => e.EventDate)
            .ToListAsync();

        return Ok(events.Select(e => e.ToResponse()).ToList());
    }
}