using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDayWeb.Filters;

/// <summary>[RoleAuthorize] = any logged-in user. [RoleAuthorize("Organiser")] = only that role.</summary>
public class RoleAuthorizeAttribute : ActionFilterAttribute
{
    private readonly string? _role;

    public RoleAuthorizeAttribute(string? role = null)
    {
        _role = role;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;

        if (session.GetInt32("UserId") == null)
        {
            var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        if (_role != null && session.GetString("Role") != _role)
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}