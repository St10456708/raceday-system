using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.Dtos;
using RaceDayApi.Models;

namespace RaceDayApi.Controllers;

[Tags("Event Enrolments")]
public class EnrolmentsController : ApiControllerBase
{
    private readonly AppDbContext _db;

    public EnrolmentsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Enter an event by enrolling in one of its categories (Participant only).</summary>
    /// <remarks>
    /// No request body is needed - the participant is taken from the session.
    /// The system records the link between the participant, the event (through the category) and the chosen category,
    /// and assigns a bib number. A participant can only be enrolled once per category.
    /// </remarks>
    /// <param name="categoryId">The category the participant wants to enter.</param>
    /// <response code="201">Enrolment created. Returns the enrolment including the bib number.</response>
    /// <response code="400">The category is full or the event is not open (not Upcoming).</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in, but not a Participant.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">Already enrolled in this category.</response>
    [HttpPost("api/categories/{categoryId:int}/enrol")]
    [SessionAuthorize("Participant")]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(int categoryId)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == categoryId);
        if (category == null)
        {
            return NotFound(new MessageResponse("Category not found."));
        }

        if (category.Event.Status != "Upcoming")
        {
            return BadRequest(new MessageResponse("This event is not open for enrolment."));
        }

        var userId = CurrentUserId!.Value;

        var existing = await _db.Enrolments.FirstOrDefaultAsync(e => e.ParticipantId == userId && e.CategoryId == categoryId);
        if (existing != null && existing.Status == "Confirmed")
        {
            return Conflict(new MessageResponse("You are already enrolled in this category."));
        }

        var confirmedCount = await _db.Enrolments.CountAsync(e => e.CategoryId == categoryId && e.Status == "Confirmed");
        if (confirmedCount >= category.MaxParticipants)
        {
            return BadRequest(new MessageResponse("This category is full."));
        }

        Enrolment enrolment;
        if (existing != null)
        {
            // The participant cancelled earlier and is now re-enrolling.
            existing.Status = "Confirmed";
            existing.EnrolmentDate = DateTime.Now;
            enrolment = existing;
        }
        else
        {
            enrolment = new Enrolment
            {
                ParticipantId = userId,
                CategoryId = categoryId,
                Status = "Confirmed",
                EnrolmentDate = DateTime.Now
            };
            _db.Enrolments.Add(enrolment);
        }

        await _db.SaveChangesAsync();

        if (string.IsNullOrEmpty(enrolment.BibNumber))
        {
            enrolment.BibNumber = $"B{enrolment.EnrolmentId:D4}";
            await _db.SaveChangesAsync();
        }

        var saved = await LoadEnrolments().FirstAsync(e => e.EnrolmentId == enrolment.EnrolmentId);
        return Created("/api/users/me/enrolments", saved.ToResponse());
    }

    /// <summary>List my own enrolments - current and past (Participant only).</summary>
    /// <response code="200">Returns an array of my enrolments, newest first.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in, but not a Participant.</response>
    [HttpGet("api/users/me/enrolments")]
    [SessionAuthorize("Participant")]
    [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var userId = CurrentUserId!.Value;
        var list = await LoadEnrolments()
            .Where(e => e.ParticipantId == userId)
            .OrderByDescending(e => e.EnrolmentDate)
            .ToListAsync();

        return Ok(list.Select(e => e.ToResponse()).ToList());
    }

    /// <summary>List all enrolments for one of my events (owning Organiser only).</summary>
    /// <param name="eventId">The event id.</param>
    /// <response code="200">Returns an array of all enrolments for the event.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the Organiser who owns this event.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("api/events/{eventId:int}/enrolments")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null)
        {
            return NotFound(new MessageResponse("Event not found."));
        }

        if (ev.OrganiserId != CurrentUserId!.Value)
        {
            return ForbiddenResult("You can only view enrolments for your own events.");
        }

        var list = await LoadEnrolments()
            .Where(e => e.Category.EventId == eventId)
            .OrderBy(e => e.EnrolmentDate)
            .ToListAsync();

        return Ok(list.Select(e => e.ToResponse()).ToList());
    }

    /// <summary>Cancel one of my enrolments (owning Participant only).</summary>
    /// <param name="id">The enrolment id.</param>
    /// <response code="204">Enrolment cancelled.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a Participant, or the enrolment belongs to someone else.</response>
    /// <response code="404">Enrolment not found.</response>
    [HttpDelete("api/enrolments/{id:int}")]
    [SessionAuthorize("Participant")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(int id)
    {
        var enrolment = await _db.Enrolments.FindAsync(id);
        if (enrolment == null)
        {
            return NotFound(new MessageResponse("Enrolment not found."));
        }

        if (enrolment.ParticipantId != CurrentUserId!.Value)
        {
            return ForbiddenResult("You can only cancel your own enrolments.");
        }

        enrolment.Status = "Cancelled";
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private IQueryable<Enrolment> LoadEnrolments() =>
        _db.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Category)
            .ThenInclude(c => c.Event);
}