using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Filters;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

[RoleAuthorize("Organiser")]
public class OrganiserController : Controller
{
    private readonly ApiClient _api;

    public OrganiserController(ApiClient api)
    {
        _api = api;
    }

    private IActionResult Fail(string message, string action = nameof(Events))
    {
        TempData["Error"] = message;
        return RedirectToAction(action);
    }

    // ---------------- Dashboard ----------------
    public async Task<IActionResult> Dashboard()
    {
        var res = await _api.GetAsync<List<EventDto>>("api/events/mine");
        if (!res.Success) ViewBag.Error = res.Error;

        var rows = new List<DashboardRow>();
        foreach (var ev in (res.Data ?? new List<EventDto>()).OrderBy(e => e.EventDate))
        {
            var enrolments = await _api.GetAsync<List<EnrolmentDto>>($"api/events/{ev.EventId}/enrolments");
            var list = enrolments.Data ?? new List<EnrolmentDto>();
            rows.Add(new DashboardRow(ev, list.Count(x => x.Status == "Confirmed"), list.Count));
        }

        return View(rows);
    }

    // ---------------- Events ----------------
    public async Task<IActionResult> Events()
    {
        var res = await _api.GetAsync<List<EventDto>>("api/events/mine");
        if (!res.Success) ViewBag.Error = res.Error;
        return View(res.Data ?? new List<EventDto>());
    }

    [HttpGet]
    public IActionResult CreateEvent() => View("EventForm", new EventFormVm());

    [HttpPost]
    public async Task<IActionResult> CreateEvent(EventFormVm vm)
    {
        if (!ModelState.IsValid) return View("EventForm", vm);

        var res = await _api.PostAsync<EventDto>("api/events", new
        {
            eventName = vm.EventName,
            description = vm.Description,
            eventDate = vm.EventDate!.Value.ToString("yyyy-MM-dd"),
            eventType = vm.EventType,
            venue = new { venueName = vm.VenueName, address = vm.Address, city = vm.City }
        });

        if (!res.Success)
        {
            ModelState.AddModelError("", res.Error);
            return View("EventForm", vm);
        }

        var id = res.Data!.EventId;

        // the "distance" of the event = its main race category
        await _api.PostAsync<CategoryDto>($"api/events/{id}/categories", new
        {
            categoryName = $"{vm.DistanceKm:0.##}km {vm.EventType}",
            distanceKm = vm.DistanceKm,
            price = vm.Price,
            maxParticipants = vm.MaxParticipants
        });

        await UploadBannerAsync(id, vm.Banner);

        if (TempData["Error"] == null) TempData["Success"] = "Event created. Add more categories below if you need them.";
        return RedirectToAction(nameof(Categories), new { eventId = id });
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var res = await _api.GetAsync<EventDto>($"api/events/{id}");
        if (!res.Success) return Fail(res.Error);

        var e = res.Data!;
        var main = e.Categories.FirstOrDefault();
        return View("EventForm", new EventFormVm
        {
            EventId = e.EventId,
            IsEdit = true,
            EventName = e.EventName,
            Description = e.Description,
            EventDate = e.EventDate.ToDateTime(TimeOnly.MinValue),
            EventType = e.EventType,
            Status = e.Status,
            VenueName = e.Venue?.VenueName ?? "",
            Address = e.Venue?.Address ?? "",
            City = e.Venue?.City ?? "",
            DistanceKm = main?.DistanceKm ?? 5,
            Price = main?.Price ?? 0,
            MaxParticipants = main?.MaxParticipants ?? 100,
            BannerUrl = e.BannerUrl
        });
    }

    [HttpPost]
    public async Task<IActionResult> EditEvent(EventFormVm vm)
    {
        vm.IsEdit = true;
        if (!ModelState.IsValid) return View("EventForm", vm);

        var res = await _api.PutAsync<EventDto>($"api/events/{vm.EventId}", new
        {
            eventName = vm.EventName,
            description = vm.Description,
            eventDate = vm.EventDate!.Value.ToString("yyyy-MM-dd"),
            eventType = vm.EventType,
            status = vm.Status,
            venue = new { venueName = vm.VenueName, address = vm.Address, city = vm.City }
        });

        if (!res.Success)
        {
            ModelState.AddModelError("", res.Error);
            return View("EventForm", vm);
        }

        var main = res.Data!.Categories.OrderBy(c => c.CategoryId).FirstOrDefault();
        if (main != null)
        {
            await _api.PutAsync<CategoryDto>($"api/categories/{main.CategoryId}", new
            {
                categoryName = main.CategoryName,
                distanceKm = vm.DistanceKm,
                price = vm.Price,
                maxParticipants = vm.MaxParticipants
            });
        }

        await UploadBannerAsync(vm.EventId, vm.Banner);

        if (TempData["Error"] == null) TempData["Success"] = "Event updated.";
        return RedirectToAction(nameof(Events));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var res = await _api.DeleteAsync($"api/events/{id}");
        if (res.Success) TempData["Success"] = "Event deleted.";
        else TempData["Error"] = res.Error;
        return RedirectToAction(nameof(Events));
    }

    private async Task UploadBannerAsync(int eventId, IFormFile? banner)
    {
        if (banner == null || banner.Length == 0) return;

        var up = await _api.PostFileAsync<EventDto>($"api/events/{eventId}/banner", banner);
        if (!up.Success) TempData["Error"] = "Event saved, but the banner was not uploaded: " + up.Error;
    }

    // ---------------- Categories ----------------
    public async Task<IActionResult> Categories(int eventId)
    {
        var res = await _api.GetAsync<EventDto>($"api/events/{eventId}");
        if (!res.Success) return Fail(res.Error);
        return View(new CategoriesPage(res.Data!, new CategoryVm { EventId = eventId }));
    }

    [HttpPost]
    public async Task<IActionResult> AddCategory(CategoryVm vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please check the category details (name, distance, price).";
            return RedirectToAction(nameof(Categories), new { eventId = vm.EventId });
        }

        var res = await _api.PostAsync<CategoryDto>($"api/events/{vm.EventId}/categories", new
        {
            categoryName = vm.CategoryName,
            distanceKm = vm.DistanceKm,
            price = vm.Price,
            maxParticipants = vm.MaxParticipants
        });

        if (res.Success) TempData["Success"] = "Category added.";
        else TempData["Error"] = res.Error;
        return RedirectToAction(nameof(Categories), new { eventId = vm.EventId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCategory(int id, int eventId)
    {
        var res = await _api.DeleteAsync($"api/categories/{id}");
        if (res.Success) TempData["Success"] = "Category deleted.";
        else TempData["Error"] = res.Error;
        return RedirectToAction(nameof(Categories), new { eventId });
    }

    // ---------------- Enrolments ----------------
    public async Task<IActionResult> Enrolments(int eventId)
    {
        var ev = await _api.GetAsync<EventDto>($"api/events/{eventId}");
        if (!ev.Success) return Fail(ev.Error);

        var en = await _api.GetAsync<List<EnrolmentDto>>($"api/events/{eventId}/enrolments");
        if (!en.Success) return Fail(en.Error);

        return View(new EnrolmentsPage(ev.Data!, en.Data ?? new List<EnrolmentDto>()));
    }

    // ---------------- Results ----------------
    public async Task<IActionResult> Results(int eventId)
    {
        var ev = await _api.GetAsync<EventDto>($"api/events/{eventId}");
        if (!ev.Success) return Fail(ev.Error);

        var en = await _api.GetAsync<List<EnrolmentDto>>($"api/events/{eventId}/enrolments");
        if (!en.Success) return Fail(en.Error);

        var rs = await _api.GetAsync<List<ResultDto>>($"api/events/{eventId}/results");
        var existing = (rs.Data ?? new List<ResultDto>()).ToDictionary(r => r.EnrolmentId);
        var confirmed = (en.Data ?? new List<EnrolmentDto>()).Where(x => x.Status == "Confirmed").ToList();

        return View(new ResultsPage(ev.Data!, confirmed, existing));
    }

    [HttpPost]
    public async Task<IActionResult> SaveResult(ResultVm vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please check the result: time must look like 00:52:14 and position must be a number.";
            return RedirectToAction(nameof(Results), new { eventId = vm.EventId });
        }

        var res = await _api.PostAsync<ResultDto>($"api/enrolments/{vm.EnrolmentId}/result", new
        {
            finishTime = string.IsNullOrWhiteSpace(vm.FinishTime) ? null : vm.FinishTime,
            position = vm.Position,
            status = vm.Status
        });

        if (res.Success) TempData["Success"] = "Result published.";
        else TempData["Error"] = res.Error;
        return RedirectToAction(nameof(Results), new { eventId = vm.EventId });
    }
}