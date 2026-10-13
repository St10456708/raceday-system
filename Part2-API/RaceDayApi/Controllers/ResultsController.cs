using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.Dtos;
using RaceDayApi.Models;

namespace RaceDayApi.Controllers;

[Tags("Results")]
public class ResultsController : ApiControllerBase
{
    private static readonly string[] ResultStatuses = { "Finished", "DNF", "DNS" };

    private readonly AppDbContext _db;

    public ResultsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Capture a finishing time and position for a participant's enrolment (Organiser of that event only).</summary>
    /// <remarks>
    /// Send finishTime (hh:mm:ss), position and status ("Finished", "DNF" or "DNS").
    /// If status is "Finished", finishTime and position are required. Only one result can be captured per enrolment.
    /// </remarks>
    /// <param name="enrolmentId">The enrolment id the result belongs to.</param>
    /// <param name="request">The result details (JSON body).</param>
    /// <response code="201">Result captured. Returns the result.</response>
    /// <response code="400">Invalid fields, or the enrolment was cancelled.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the Organiser of this event.</response>
    /// <response code="404">Enrolment not found.</response>
    /// <response code="409">A result has already been captured for this enrolment.</response>
    [HttpPost("api/enrolments/{enrolmentId:int}/result")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(int enrolmentId, [FromBody] ResultRequest request)
    {
        var enrolment = await _db.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Category)
            .ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentId == enrolmentId);

        if (enrolment == null)
        {
            return NotFound(new MessageResponse("Enrolment not found."));
        }

        if (enrolment.Category.Event.OrganiserId != CurrentUserId!.Value)
        {
            return ForbiddenResult("You can only capture results for your own events.");
        }

        if (!ResultStatuses.Contains(request.Status))
        {
            return BadRequest(new MessageResponse("Status must be 'Finished', 'DNF' or 'DNS'."));
        }

        if (enrolment.Status == "Cancelled")
        {
            return BadRequest(new MessageResponse("This enrolment was cancelled."));
        }

        TimeSpan? finishTime = null;
        if (!string.IsNullOrWhiteSpace(request.FinishTime))
        {
            if (!TimeSpan.TryParseExact(request.FinishTime, @"hh\:mm\:ss", CultureInfo.InvariantCulture, out var parsed))
            {
                return BadRequest(new MessageResponse("FinishTime must be in the format hh:mm:ss, for example 00:52:14."));
            }

            finishTime = parsed;
        }

        if (request.Status == "Finished" && (finishTime == null || request.Position == null))
        {
            return BadRequest(new MessageResponse("A 'Finished' result needs both finishTime and position."));
        }

        if (await _db.Results.AnyAsync(r => r.EnrolmentId == enrolmentId))
        {
            return Conflict(new MessageResponse("A result has already been captured for this enrolment."));
        }

        var result = new RaceResult
        {
            EnrolmentId = enrolmentId,
            FinishTime = finishTime,
            Position = request.Position,
            Status = request.Status,
            RecordedAt = DateTime.Now,
            Enrolment = enrolment
        };

        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        return Created($"/api/events/{enrolment.Category.EventId}/results", result.ToResponse());
    }

    /// <summary>List all results for an event - the leaderboard (public).</summary>
    /// <param name="eventId">The event id.</param>
    /// <response code="200">Returns an array of results, best position first.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("api/events/{eventId:int}/results")]
    [ProducesResponseType(typeof(List<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
        {
            return NotFound(new MessageResponse("Event not found."));
        }

        var list = await LoadResults()
            .Where(r => r.Enrolment.Category.EventId == eventId)
            .OrderBy(r => r.Position ?? int.MaxValue)
            .ToListAsync();

        return Ok(list.Select(r => r.ToResponse()).ToList());
    }

    /// <summary>View my own results across all events (Participant only).</summary>
    /// <response code="200">Returns an array of my results.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in, but not a Participant.</response>
    [HttpGet("api/users/me/results")]
    [SessionAuthorize("Participant")]
    [ProducesResponseType(typeof(List<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var userId = CurrentUserId!.Value;
        var list = await LoadResults()
            .Where(r => r.Enrolment.ParticipantId == userId)
            .OrderByDescending(r => r.RecordedAt)
            .ToListAsync();

        return Ok(list.Select(r => r.ToResponse()).ToList());
    }

    private IQueryable<RaceResult> LoadResults() =>
        _db.Results
            .Include(r => r.Enrolment)
            .ThenInclude(e => e.Participant)
            .Include(r => r.Enrolment)
            .ThenInclude(e => e.Category)
            .ThenInclude(c => c.Event);
}