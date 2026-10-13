using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDayApi.Data;
using RaceDayApi.Dtos;

namespace RaceDayApi.Controllers;

[Route("api/users")]
[Tags("User Profile")]
[SessionAuthorize]
public class UsersController : ApiControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>View my own profile (Organiser or Participant).</summary>
    /// <response code="200">Returns the logged-in user's profile.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe()
    {
        var user = await _db.Users.FindAsync(CurrentUserId!.Value);
        if (user == null)
        {
            return Unauthorized(new MessageResponse("Your account no longer exists."));
        }

        return Ok(new UserResponse(user.UserId, user.FullName, user.Email, user.Role, user.PhoneNumber));
    }

    /// <summary>Update my own profile (full name and phone number).</summary>
    /// <remarks>Send fullName (required) and phoneNumber (optional). Email, password and role cannot be changed here.</remarks>
    /// <response code="200">Returns the updated profile.</response>
    /// <response code="400">Invalid data.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request)
    {
        var user = await _db.Users.FindAsync(CurrentUserId!.Value);
        if (user == null)
        {
            return Unauthorized(new MessageResponse("Your account no longer exists."));
        }

        user.FullName = request.FullName.Trim();
        user.PhoneNumber = request.PhoneNumber;
        await _db.SaveChangesAsync();

        return Ok(new UserResponse(user.UserId, user.FullName, user.Email, user.Role, user.PhoneNumber));
    }
}
