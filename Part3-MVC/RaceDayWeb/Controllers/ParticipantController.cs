using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Filters;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

[RoleAuthorize("Participant")]
public class ParticipantController : Controller
{
    private readonly ApiClient _api;

    public ParticipantController(ApiClient api)
    {
        _api = api;
    }

    [HttpPost]
    public async Task<IActionResult> Enrol(int categoryId, int eventId)
    {
        var res = await _api.PostAsync<EnrolmentDto>($"api/categories/{categoryId}/enrol");
        if (res.Success)
        {
            TempData["Success"] = $"You're in! Your bib number is {res.Data!.BibNumber}.";
            return RedirectToAction(nameof(MyEvents));
        }

        TempData["Error"] = res.Error;
        return RedirectToAction("Details", "Home", new { id = eventId });
    }

    public async Task<IActionResult> MyEvents()
    {
        var res = await _api.GetAsync<List<EnrolmentDto>>("api/users/me/enrolments");
        if (!res.Success) ViewBag.Error = res.Error;
        return View(res.Data ?? new List<EnrolmentDto>());
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        var res = await _api.DeleteAsync($"api/enrolments/{id}");
        if (res.Success) TempData["Success"] = "Enrolment cancelled.";
        else TempData["Error"] = res.Error;
        return RedirectToAction(nameof(MyEvents));
    }

    public async Task<IActionResult> MyResults()
    {
        var res = await _api.GetAsync<List<ResultDto>>("api/users/me/results");
        if (!res.Success) ViewBag.Error = res.Error;

        var rows = new List<ResultRow>();
        foreach (var r in res.Data ?? new List<ResultDto>())
        {
            var ev = await _api.GetAsync<EventDto>($"api/events/{r.EventId}");
            var all = await _api.GetAsync<List<ResultDto>>($"api/events/{r.EventId}/results");
            var finishers = all.Data?.Count(x => x.Status == "Finished") ?? 0;
            rows.Add(new ResultRow(r, ev.Data?.EventDate, finishers));
        }

        return View(rows);
    }
}