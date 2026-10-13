using System.Net;
using Microsoft.Extensions.DependencyInjection;
using RaceDayApi.Data;
using Xunit;
using static RaceDayApi.Tests.TestHelpers;

namespace RaceDayApi.Tests;

public class EnrolmentTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public EnrolmentTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Participant_CanEnrol_AndEnrolmentIsRecorded()
    {
        var (organiser, _, _) = await LoginAsNewUser(_factory, "Organiser");
        var eventId = await CreateEvent(organiser);
        var categoryId = await CreateCategory(organiser, eventId);
        var (participant, participantId, _) = await LoginAsNewUser(_factory, "Participant");

        var response = await participant.PostAsync($"/api/categories/{categoryId}/enrol", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await ReadJson(response);
        Assert.Equal(categoryId, json.GetProperty("categoryId").GetInt32());
        Assert.Equal(eventId, json.GetProperty("eventId").GetInt32());
        Assert.False(string.IsNullOrEmpty(json.GetProperty("bibNumber").GetString()));

        // The enrolment is really stored in the database and links participant + category.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Single(db.Enrolments.Where(e => e.ParticipantId == participantId && e.CategoryId == categoryId));
    }

    [Fact]
    public async Task Participant_SeesEnrolmentInTheirOwnList()
    {
        var (organiser, _, _) = await LoginAsNewUser(_factory, "Organiser");
        var eventId = await CreateEvent(organiser);
        var categoryId = await CreateCategory(organiser, eventId);
        var (participant, _, _) = await LoginAsNewUser(_factory, "Participant");
        var enrolmentId = await Enrol(participant, categoryId);

        var response = await participant.GetAsync("/api/users/me/enrolments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJson(response);
        Assert.Equal(1, json.GetArrayLength());
        Assert.Equal(enrolmentId, json[0].GetProperty("enrolmentId").GetInt32());
    }

    [Fact]
    public async Task Participant_EnrollingTwiceInSameCategory_Returns409()
    {
        var (organiser, _, _) = await LoginAsNewUser(_factory, "Organiser");
        var eventId = await CreateEvent(organiser);
        var categoryId = await CreateCategory(organiser, eventId);
        var (participant, _, _) = await LoginAsNewUser(_factory, "Participant");

        await Enrol(participant, categoryId);
        var second = await participant.PostAsync($"/api/categories/{categoryId}/enrol", null);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_InFullCategory_Returns400()
    {
        var (organiser, _, _) = await LoginAsNewUser(_factory, "Organiser");
        var eventId = await CreateEvent(organiser);
        var categoryId = await CreateCategory(organiser, eventId, maxParticipants: 1);
        var (first, _, _) = await LoginAsNewUser(_factory, "Participant");
        var (second, _, _) = await LoginAsNewUser(_factory, "Participant");

        await Enrol(first, categoryId);
        var response = await second.PostAsync($"/api/categories/{categoryId}/enrol", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_InCategoryThatDoesNotExist_Returns404()
    {
        var (participant, _, _) = await LoginAsNewUser(_factory, "Participant");

        var response = await participant.PostAsync("/api/categories/999999/enrol", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotEnrol_Returns403()
    {
        var (organiser, _, _) = await LoginAsNewUser(_factory, "Organiser");
        var eventId = await CreateEvent(organiser);
        var categoryId = await CreateCategory(organiser, eventId);

        var response = await organiser.PostAsync($"/api/categories/{categoryId}/enrol", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotEnrol_Returns401()
    {
        var (organiser, _, _) = await LoginAsNewUser(_factory, "Organiser");
        var eventId = await CreateEvent(organiser);
        var categoryId = await CreateCategory(organiser, eventId);
        var anonymous = _factory.CreateClient();

        var response = await anonymous.PostAsync($"/api/categories/{categoryId}/enrol", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanViewAllEnrolmentsForOwnEvent()
    {
        var (organiser, _, _) = await LoginAsNewUser(_factory, "Organiser");
        var eventId = await CreateEvent(organiser);
        var categoryId = await CreateCategory(organiser, eventId);
        var (p1, _, _) = await LoginAsNewUser(_factory, "Participant");
        var (p2, _, _) = await LoginAsNewUser(_factory, "Participant");
        await Enrol(p1, categoryId);
        await Enrol(p2, categoryId);

        var response = await organiser.GetAsync($"/api/events/{eventId}/enrolments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, (await ReadJson(response)).GetArrayLength());
    }

    [Fact]
    public async Task Participant_CannotViewEventEnrolments_Returns403()
    {
        var (organiser, _, _) = await LoginAsNewUser(_factory, "Organiser");
        var eventId = await CreateEvent(organiser);
        var (participant, _, _) = await LoginAsNewUser(_factory, "Participant");

        var response = await participant.GetAsync($"/api/events/{eventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OtherOrganiser_CannotViewEventEnrolments_Returns403()
    {
        var (owner, _, _) = await LoginAsNewUser(_factory, "Organiser");
        var eventId = await CreateEvent(owner);
        var (otherOrganiser, _, _) = await LoginAsNewUser(_factory, "Organiser");

        var response = await otherOrganiser.GetAsync($"/api/events/{eventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CanCancelOwnEnrolment_Returns204()
    {
        var (organiser, _, _) = await LoginAsNewUser(_factory, "Organiser");
        var eventId = await CreateEvent(organiser);
        var categoryId = await CreateCategory(organiser, eventId);
        var (participant, _, _) = await LoginAsNewUser(_factory, "Participant");
        var enrolmentId = await Enrol(participant, categoryId);

        var response = await participant.DeleteAsync($"/api/enrolments/{enrolmentId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCancelSomeoneElsesEnrolment_Returns403()
    {
        var (organiser, _, _) = await LoginAsNewUser(_factory, "Organiser");
        var eventId = await CreateEvent(organiser);
        var categoryId = await CreateCategory(organiser, eventId);
        var (owner, _, _) = await LoginAsNewUser(_factory, "Participant");
        var (other, _, _) = await LoginAsNewUser(_factory, "Participant");
        var enrolmentId = await Enrol(owner, categoryId);

        var response = await other.DeleteAsync($"/api/enrolments/{enrolmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
