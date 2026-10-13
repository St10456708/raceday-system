using System.Net;
using System.Net.Http.Json;
using Xunit;
using static RaceDayApi.Tests.TestHelpers;

namespace RaceDayApi.Tests;

public class ResultTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ResultTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static object FinishedResult => new { finishTime = "00:52:14", position = 5, status = "Finished" };

    [Fact]
    public async Task Organiser_CanCaptureResult_AndParticipantCanViewIt()
    {
        var (organiser, _, _) = await LoginAsNewUser(_factory, "Organiser");
        var eventId = await CreateEvent(organiser);
        var categoryId = await CreateCategory(organiser, eventId);
        var (participant, _, _) = await LoginAsNewUser(_factory, "Participant");
        var enrolmentId = await Enrol(participant, categoryId);

        var capture = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", FinishedResult);
        var mine = await participant.GetAsync("/api/users/me/results");

        Assert.Equal(HttpStatusCode.Created, capture.StatusCode);
        Assert.Equal(HttpStatusCode.OK, mine.StatusCode);
        var json = await ReadJson(mine);
        Assert.Equal(1, json.GetArrayLength());
        Assert.Equal("00:52:14", json[0].GetProperty("finishTime").GetString());
        Assert.Equal(5, json[0].GetProperty("position").GetInt32());
    }

    [Fact]
    public async Task EventResults_ArePublic_AndListCapturedResults()
    {
        var (organiser, _, _) = await LoginAsNewUser(_factory, "Organiser");
        var eventId = await CreateEvent(organiser);
        var categoryId = await CreateCategory(organiser, eventId);
        var (participant, _, _) = await LoginAsNewUser(_factory, "Participant");
        var enrolmentId = await Enrol(participant, categoryId);
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", FinishedResult);
        var anonymous = _factory.CreateClient();

        var response = await anonymous.GetAsync($"/api/events/{eventId}/results");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, (await ReadJson(response)).GetArrayLength());
    }

    [Fact]
    public async Task Participant_CannotCaptureResult_Returns403()
    {
        var (organiser, _, _) = await LoginAsNewUser(_factory, "Organiser");
        var eventId = await CreateEvent(organiser);
        var categoryId = await CreateCategory(organiser, eventId);
        var (participant, _, _) = await LoginAsNewUser(_factory, "Participant");
        var enrolmentId = await Enrol(participant, categoryId);

        var response = await participant.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", FinishedResult);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OtherOrganiser_CannotCaptureResult_Returns403()
    {
        var (owner, _, _) = await LoginAsNewUser(_factory, "Organiser");
        var eventId = await CreateEvent(owner);
        var categoryId = await CreateCategory(owner, eventId);
        var (participant, _, _) = await LoginAsNewUser(_factory, "Participant");
        var enrolmentId = await Enrol(participant, categoryId);
        var (otherOrganiser, _, _) = await LoginAsNewUser(_factory, "Organiser");

        var response = await otherOrganiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", FinishedResult);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_ForUnknownEnrolment_Returns404()
    {
        var (organiser, _, _) = await LoginAsNewUser(_factory, "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/enrolments/999999/result", FinishedResult);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_Twice_Returns409()
    {
        var (organiser, _, _) = await LoginAsNewUser(_factory, "Organiser");
        var eventId = await CreateEvent(organiser);
        var categoryId = await CreateCategory(organiser, eventId);
        var (participant, _, _) = await LoginAsNewUser(_factory, "Participant");
        var enrolmentId = await Enrol(participant, categoryId);

        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", FinishedResult);
        var second = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", FinishedResult);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_WithBadTimeFormat_Returns400()
    {
        var (organiser, _, _) = await LoginAsNewUser(_factory, "Organiser");
        var eventId = await CreateEvent(organiser);
        var categoryId = await CreateCategory(organiser, eventId);
        var (participant, _, _) = await LoginAsNewUser(_factory, "Participant");
        var enrolmentId = await Enrol(participant, categoryId);

        var response = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result",
            new { finishTime = "fast", position = 1, status = "Finished" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotViewParticipantResultsEndpoint_Returns403()
    {
        var (organiser, _, _) = await LoginAsNewUser(_factory, "Organiser");

        var response = await organiser.GetAsync("/api/users/me/results");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}