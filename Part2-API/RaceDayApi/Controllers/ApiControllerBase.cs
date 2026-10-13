using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDayApi.Dtos;

namespace RaceDayApi.Controllers;

/// <summary>
/// Put this above a controller or an action to protect it.
/// [SessionAuthorize] = any logged-in user.
/// [SessionAuthorize("Organiser")] = only logged-in Organisers.
/// The user id and role are read from the SERVER-SIDE SESSION that was created at login.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string? _requiredRole;

    public SessionAuthorizeAttribute(string? requiredRole = null)
    {
        _requiredRole = requiredRole;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var userId = context.HttpContext.Session.GetInt32("UserId");
        var role = context.HttpContext.Session.GetString("Role");

        if (userId == null || role == null)
        {
            context.Result = new ObjectResult(new MessageResponse("You must log in first."))
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
            return;
        }

        if (_requiredRole != null && role != _requiredRole)
        {
            context.Result = new ObjectResult(new MessageResponse($"This action is only available to the {_requiredRole} role."))
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected int? CurrentUserId => HttpContext.Session.GetInt32("UserId");

    protected string? CurrentRole => HttpContext.Session.GetString("Role");

    protected ObjectResult ForbiddenResult(string message) =>
        StatusCode(StatusCodes.Status403Forbidden, new MessageResponse(message));
}