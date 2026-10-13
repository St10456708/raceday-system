using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.Dtos;
using RaceDayApi.Models;

namespace RaceDayApi.Controllers;

[Route("api/auth")]
[Tags("Authentication")]
public class AuthController : ApiControllerBase
{
    private readonly AppDbContext _db;

    public AuthController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Register a new account as an Organiser or a Participant.</summary>
    /// <remarks>
    /// Send fullName, email, password (minimum 6 characters), role and an optional phoneNumber.
    /// The role must be exactly "Organiser" or "Participant" - any other value is rejected.
    /// The password is hashed with BCrypt and never stored in its original form.
    /// </remarks>
    /// <response code="201">Account created. Returns the new user id and role. Now call POST /api/auth/login.</response>
    /// <response code="400">A field is missing or invalid, or the role is not Organiser/Participant.</response>
    /// <response code="409">That email address is already registered.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (request.Role != "Organiser" && request.Role != "Participant")
        {
            return BadRequest(new MessageResponse("Role must be exactly 'Organiser' or 'Participant'."));
        }

        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email))
        {
            return Conflict(new MessageResponse("That email address is already registered."));
        }

        var user = new AppUser
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            PhoneNumber = request.PhoneNumber,
            CreatedAt = DateTime.Now
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Created("/api/users/me",
            new AuthResponse(user.UserId, user.FullName, user.Email, user.Role, "Registration successful. Please log in."));
    }

    /// <summary>Log in with email and password.</summary>
    /// <remarks>
    /// On success the server creates a SESSION that stores your user id and role.
    /// The browser keeps the session cookie automatically, so every later request (including Swagger "Try it out") is authenticated.
    /// </remarks>
    /// <response code="200">Logged in. Returns the user id and role.</response>
    /// <response code="400">Email or password missing.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null || !PasswordMatches(request.Password, user.PasswordHash))
        {
            return Unauthorized(new MessageResponse("Invalid email or password."));
        }

        HttpContext.Session.SetInt32("UserId", user.UserId);
        HttpContext.Session.SetString("Role", user.Role);

        return Ok(new AuthResponse(user.UserId, user.FullName, user.Email, user.Role, "Login successful."));
    }

    /// <summary>Log out and destroy the current session.</summary>
    /// <response code="200">Session cleared.</response>
    [HttpPost("logout")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new MessageResponse("Logged out."));
    }

    private static bool PasswordMatches(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }
}