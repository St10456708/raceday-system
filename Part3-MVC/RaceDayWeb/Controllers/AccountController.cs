using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Filters;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

public class AccountController : Controller
{
    private readonly ApiClient _api;

    public AccountController(ApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterVm());

    [HttpPost]
    public async Task<IActionResult> Register(RegisterVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var res = await _api.PostAsync<AuthResponse>("api/auth/register", new
        {
            fullName = vm.FullName,
            email = vm.Email,
            password = vm.Password,
            role = vm.Role,
            phoneNumber = vm.PhoneNumber
        });

        if (!res.Success)
        {
            ModelState.AddModelError("", res.Error);
            return View(vm);
        }

        TempData["Success"] = "Account created. Please log in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl) => View(new LoginVm { ReturnUrl = returnUrl });

    [HttpPost]
    public async Task<IActionResult> Login(LoginVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        HttpContext.Session.Clear();
        var res = await _api.PostAsync<AuthResponse>("api/auth/login", new { email = vm.Email, password = vm.Password });

        if (!res.Success)
        {
            ModelState.AddModelError("", res.Error);
            return View(vm);
        }

        var user = res.Data!;
        HttpContext.Session.SetInt32("UserId", user.UserId);
        HttpContext.Session.SetString("Role", user.Role);
        HttpContext.Session.SetString("FullName", user.FullName);

        if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
            return LocalRedirect(vm.ReturnUrl);

        return user.Role == "Organiser"
            ? RedirectToAction("Dashboard", "Organiser")
            : RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _api.PostAsync<object>("api/auth/logout");
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }

    public IActionResult AccessDenied() => View();

    [RoleAuthorize, HttpGet]
    public async Task<IActionResult> Profile()
    {
        var res = await _api.GetAsync<UserDto>("api/users/me");
        if (!res.Success)
        {
            TempData["Error"] = res.Error;
            return RedirectToAction("Index", "Home");
        }

        var u = res.Data!;
        HttpContext.Session.SetString("Pic", u.ProfilePictureUrl ?? "");
        return View(new ProfileVm
        {
            FullName = u.FullName,
            Email = u.Email,
            Role = u.Role,
            PhoneNumber = u.PhoneNumber,
            ProfilePictureUrl = u.ProfilePictureUrl
        });
    }

    [RoleAuthorize, HttpPost]
    public async Task<IActionResult> Profile(ProfileVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var res = await _api.PutAsync<UserDto>("api/users/me", new { fullName = vm.FullName, phoneNumber = vm.PhoneNumber });
        if (!res.Success)
        {
            ModelState.AddModelError("", res.Error);
            return View(vm);
        }

        HttpContext.Session.SetString("FullName", res.Data!.FullName);
        TempData["Success"] = "Profile updated.";
        return RedirectToAction(nameof(Profile));
    }

    [RoleAuthorize, HttpPost]
    public async Task<IActionResult> UploadPicture(IFormFile? picture)
    {
        if (picture == null || picture.Length == 0)
        {
            TempData["Error"] = "Please choose an image first.";
            return RedirectToAction(nameof(Profile));
        }

        var res = await _api.PostFileAsync<UserDto>("api/users/me/picture", picture);
        if (res.Success)
        {
            HttpContext.Session.SetString("Pic", res.Data!.ProfilePictureUrl ?? "");
            TempData["Success"] = "Profile picture updated.";
        }
        else
        {
            TempData["Error"] = res.Error;
        }

        return RedirectToAction(nameof(Profile));
    }
}