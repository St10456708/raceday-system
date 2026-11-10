using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

public class HomeController : Controller
{
    private readonly ApiClient _api;

    public HomeController(ApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(string? q, string? type, string? city, DateTime? from)
    {
        var res = await _api.GetAsync<List<EventDto>>("api/events");
        var events = res.Data ?? new List<EventDto>();
        if (!res.Success) ViewBag.Error = res.Error;

        if (!string.IsNullOrWhiteSpace(q))
            events = events.Where(e => e.EventName.Contains(q, StringComparison.OrdinalIgnoreCase)
                                    || (e.Description ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(type))
            events = events.Where(e => e.EventType == type).ToList();
        if (!string.IsNullOrWhiteSpace(city))
            events = events.Where(e => e.Venue != null && e.Venue.City.Contains(city, StringComparison.OrdinalIgnoreCase)).ToList();
        if (from.HasValue)
            events = events.Where(e => e.EventDate >= DateOnly.FromDateTime(from.Value)).ToList();

        ViewBag.Q = q;
        ViewBag.Type = type;
        ViewBag.City = city;
        ViewBag.From = from?.ToString("yyyy-MM-dd");
        return View(events);
    }

    public async Task<IActionResult> Details(int id)
    {
        var res = await _api.GetAsync<EventDto>($"api/events/{id}");
        if (!res.Success)
        {
            TempData["Error"] = res.Error;
            return RedirectToAction(nameof(Index));
        }

        return View(res.Data);
    }

    public IActionResult Error() => View();
}