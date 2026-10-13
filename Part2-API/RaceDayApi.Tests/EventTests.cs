using System.Net;
using System.Net.Http.Json;
using Xunit;
using static RaceDayApi.Tests.TestHelpers;

namespace RaceDayApi.Tests;

public class EventTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public EventTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static object NewEventBody(string type = "Run") => new
    {
        eventName = "Unit Test Race",
        description = "Created by a unit test",
        eventDate = "2026-12-01",
        eventType = type,
        venue = new { venueName = "Test Park", address = "1 Main Street", city = "Pretoria" }
    };

    // ---------- Organisers can manage events ----------

    [Fact]
    public async Task Organiser_CanCreateEvent_Returns201()
    {
        var (organiser, _, _) = await LoginAsNewUser(_factory, "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", NewEventBody());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await ReadJson(response);
        Assert.Equal("Unit Test Race", json.GetProperty("eventName").GetString());
        Assert.Equal("Pretoria", json.GetProperty("venue").GetProperty("city").GetString());
    }

    [Fact]
    public async Task Organiser_CreateEventWithInvalidType_Returns400()
    {
        var (organiser, _, _) = await LoginAsNewUser(_factory, "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", NewEventBody("Swim"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnEvent_Returns200()
    {
        var (organiser, _, _) = await LoginAsNewUser(_factory, "Organiser");
        var eventId = await CreateEvent(organiser);

        var response = await organiser.PutAsJsonAsync($"/api/events/{eventId}", new
        {
            eventName = "Renamed Race",
            description = "Updated",
            eventDate = "2026-12-02",
            eventType = "Cycle",
            status = "Upcoming"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJson(response);
        Assert.Equal("Renamed Race", json.GetProperty("eventName").GetString());
        Assert.Equal("Cycle", json.GetProperty("eventType").GetString());
    }

    [Fact]
    public async Task Organiser_CanDeleteOwnEvent_Returns204_ThenEventIsGone()
    {
        var (organiser, _, _) = await LoginAsNewUser(_factory, "Organiser");
        var eventId = await CreateEvent(organiser);

        var delete = await organiser.DeleteAsync($"/api/events/{eventId}");
        var get = await organiser.GetAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanCreateAndListCategories()
    {
        var (organiser, _, _) = await LoginAsNewUser(_factory, "Organiser");
        var eventId = await CreateEvent(organiser);

        var create = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new
        {
            categoryName = "Under 20",
            distanceKm = 5,
            price = 80,
            maxParticipants = 50
        });
        var list = await organiser.GetAsync($"/api/events/{eventId}/categories");

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var json = await ReadJson(list);
        Assert.Equal(1, json.GetArrayLength());
        Assert.Equal("Under 20", json[0].GetProperty("categoryName").GetString());
    }

    // ---------- Both roles (and the public) can view events and categories ----------

    [Fact]
    public async Task Participant_CanViewEventsAndCategories()
    {
        var (organiser, _, _) = await LoginAsNewUser(_factory, "Organiser");
        var eventId = await CreateEvent(organiser);
        await CreateCategory(organiser, eventId);
        var (participant, _, _) = await LoginAsNewUser(_factory, "Participant");

        var events = await participant.GetAsync("/api/events");
        var single = await participant.GetAsync($"/api/events/{eventId}");
        var categories = await participant.GetAsync($"/api/events/{eventId}/categories");

        Assert.Equal(HttpStatusCode.OK, events.StatusCode);
        Assert.Equal(HttpStatusCode.OK, single.StatusCode);
        Assert.Equal(HttpStatusCode.OK, categories.StatusCode);
    }

    [Fact]
    public async Task GetEvent_ThatDoesNotExist_Returns404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/events/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- Different roles are rejected from role-based functionality ----------

    [Fact]
    public async Task Participant_CannotCreateEvent_Returns403()
    {
        var (participant, _, _) = await LoginAsNewUser(_factory, "Participant");

        var response = await participant.PostAsJsonAsync("/api/events", NewEventBody());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotCreateEvent_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/events", NewEventBody());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotUpdateOrDeleteEvent_Returns403()
    {
        var (organiser, _, _) = await LoginAsNewUser(_factory, "Organiser");
        var eventId = await CreateEvent(organiser);
        var (participant, _, _) = await LoginAsNewUser(_factory, "Participant");

        var update = await participant.PutAsJsonAsync($"/api/events/{eventId}", new
        {
            eventName = "Hacked",
            description = "x",
            eventDate = "2026-12-02",
            eventType = "Run",
            status = "Upcoming"
        });
        var delete = await participant.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCreateCategory_Returns403()
    {
        var (organiser, _, _) = await LoginAsNewUser(_factory, "Organiser");
        var eventId = await CreateEvent(organiser);
        var (participant, _, _) = await LoginAsNewUser(_factory, "Participant");

        var response = await participant.PostAsJsonAsync($"/api/events/{eventId}/categories", new
        {
            categoryName = "Sneaky",
            distanceKm = 5,
            price = 0,
            maxParticipants = 10
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OtherOrganiser_CannotUpdateOrDeleteSomeoneElsesEvent_Returns403()
    {
        var (owner, _, _) = await LoginAsNewUser(_factory, "Organiser");
        var eventId = await CreateEvent(owner);
        var (otherOrganiser, _, _) = await LoginAsNewUser(_factory, "Organiser");

        var update = await otherOrganiser.PutAsJsonAsync($"/api/events/{eventId}", new
        {
            eventName = "Stolen",
            description = "x",
            eventDate = "2026-12-02",
            eventType = "Run",
            status = "Upcoming"
        });
        var delete = await otherOrganiser.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }
}