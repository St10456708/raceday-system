using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.Dtos;
using RaceDayApi.Models;

namespace RaceDayApi.Controllers;

[Route("api/events")]
[Tags("Events")]
public class EventsController : ApiControllerBase
{
    private static readonly string[] EventTypes = { "Run", "Walk", "Cycle" };
    private static readonly string[] EventStatuses = { "Upcoming", "Completed", "Cancelled" };

    private readonly AppDbContext _db;

    public EventsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Create a new event (Organiser only).</summary>
    /// <remarks>
    /// Send eventName, description, eventDate (yyyy-MM-dd), eventType ("Run", "Walk" or "Cycle")
    /// and the venue (venueName, address, city). The event is owned by the logged-in Organiser.
    /// </remarks>
    /// <response code="201">Event created. Returns the event with its id.</response>
    /// <response code="400">Missing or invalid fields.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in, but not an Organiser.</response>
    [HttpPost]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request)
    {
        if (!EventTypes.Contains(request.EventType))
        {
            return BadRequest(new MessageResponse("EventType must be 'Run', 'Walk' or 'Cycle'."));
        }

        var newEvent = new RaceEvent
        {
            OrganiserId = CurrentUserId!.Value,
            EventName = request.EventName.Trim(),
            Description = request.Description,
            EventDate = request.EventDate!.Value,
            EventType = request.EventType,
            Status = "Upcoming",
            CreatedAt = DateTime.Now,
            Venue = new Venue
            {
                VenueName = request.Venue.VenueName.Trim(),
                Address = request.Venue.Address.Trim(),
                City = request.Venue.City.Trim(),
                Latitude = request.Venue.Latitude,
                Longitude = request.Venue.Longitude
            }
        };

        _db.Events.Add(newEvent);
        await _db.SaveChangesAsync();

        return Created($"/api/events/{newEvent.EventId}", newEvent.ToResponse());
    }

    /// <summary>List all upcoming events (public - no login needed).</summary>
    /// <response code="200">Returns an array of upcoming events (with venue and categories), earliest first.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<EventResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var events = await _db.Events
            .Include(e => e.Venue)
            .Include(e => e.Categories)
            .Where(e => e.Status == "Upcoming")
            .OrderBy(e => e.EventDate)
            .ToListAsync();

        return Ok(events.Select(e => e.ToResponse()).ToList());
    }

    /// <summary>Get the full details of one event, including its venue and categories (public).</summary>
    /// <param name="id">The event id.</param>
    /// <response code="200">Returns the event.</response>
    /// <response code="404">No event with that id.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var ev = await _db.Events
            .Include(e => e.Venue)
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (ev == null)
        {
            return NotFound(new MessageResponse("Event not found."));
        }

        return Ok(ev.ToResponse());
    }

    /// <summary>Update an event (only the Organiser who created it).</summary>
    /// <remarks>Send eventName, description, eventDate, eventType and status ("Upcoming", "Completed" or "Cancelled").</remarks>
    /// <param name="id">The event id.</param>
    /// <param name="request">The new event details (JSON body).</param>
    /// <response code="200">Returns the updated event.</response>
    /// <response code="400">Missing or invalid fields.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the Organiser who owns this event.</response>
    /// <response code="404">No event with that id.</response>
    [HttpPut("{id:int}")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEventRequest request)
    {
        var ev = await _db.Events
            .Include(e => e.Venue)
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (ev == null)
        {
            return NotFound(new MessageResponse("Event not found."));
        }

        if (ev.OrganiserId != CurrentUserId!.Value)
        {
            return ForbiddenResult("You can only change your own events.");
        }

        if (!EventTypes.Contains(request.EventType))
        {
            return BadRequest(new MessageResponse("EventType must be 'Run', 'Walk' or 'Cycle'."));
        }

        if (!EventStatuses.Contains(request.Status))
        {
            return BadRequest(new MessageResponse("Status must be 'Upcoming', 'Completed' or 'Cancelled'."));
        }

        ev.EventName = request.EventName.Trim();
        ev.Description = request.Description;
        ev.EventDate = request.EventDate!.Value;
        ev.EventType = request.EventType;
        ev.Status = request.Status;
        await _db.SaveChangesAsync();

        return Ok(ev.ToResponse());
    }

    /// <summary>Delete an event (only the Organiser who created it).</summary>
    /// <remarks>The event's venue and categories are deleted with it. An event that already has enrolments cannot be deleted - set its status to "Cancelled" instead.</remarks>
    /// <param name="id">The event id.</param>
    /// <response code="204">Deleted successfully.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the Organiser who owns this event.</response>
    /// <response code="404">No event with that id.</response>
    /// <response code="409">The event already has enrolments.</response>
    [HttpDelete("{id:int}")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events
            .Include(e => e.Venue)
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (ev == null)
        {
            return NotFound(new MessageResponse("Event not found."));
        }

        if (ev.OrganiserId != CurrentUserId!.Value)
        {
            return ForbiddenResult("You can only delete your own events.");
        }

        var categoryIds = ev.Categories.Select(c => c.CategoryId).ToList();
        if (await _db.Enrolments.AnyAsync(en => categoryIds.Contains(en.CategoryId)))
        {
            return Conflict(new MessageResponse("This event already has enrolments and cannot be deleted. Set its status to 'Cancelled' instead."));
        }

        if (ev.Venue != null)
        {
            _db.Venues.Remove(ev.Venue);
        }

        _db.Categories.RemoveRange(ev.Categories);
        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}