using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDayApi.Data;

namespace RaceDayApi.Tests;

/// <summary>
/// Starts the real API in memory for the tests, but swaps SQL Server for an in-memory database
/// so the tests never touch your real database.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = "RaceDayTests_" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var existing = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (existing != null)
            {
                services.Remove(existing);
            }

            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }
}

/// <summary>Small helper methods shared by all test classes.</summary>
public static class TestHelpers
{
    public const string Password = "Passw0rd!";

    public static string UniqueEmail() => $"user_{Guid.NewGuid():N}@test.com";

    public static async Task<JsonElement> ReadJson(HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    /// <summary>Registers a new user and logs them in. The returned client keeps the session cookie.</summary>
    public static async Task<(HttpClient Client, int UserId, string Email)> LoginAsNewUser(ApiFactory factory, string role)
    {
        var client = factory.CreateClient();
        var email = UniqueEmail();

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Test " + role,
            email,
            password = Password,
            role,
            phoneNumber = "0821234567"
        });
        register.EnsureSuccessStatusCode();
        var userId = (await ReadJson(register)).GetProperty("userId").GetInt32();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        login.EnsureSuccessStatusCode();

        return (client, userId, email);
    }

    public static async Task<int> CreateEvent(HttpClient organiser, string name = "Test Race")
    {
        var response = await organiser.PostAsJsonAsync("/api/events", new
        {
            eventName = name,
            description = "A test event",
            eventDate = "2026-12-01",
            eventType = "Run",
            venue = new { venueName = "Test Park", address = "1 Main Street", city = "Pretoria" }
        });
        response.EnsureSuccessStatusCode();
        return (await ReadJson(response)).GetProperty("eventId").GetInt32();
    }

    public static async Task<int> CreateCategory(HttpClient organiser, int eventId, int maxParticipants = 100)
    {
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new
        {
            categoryName = "10km Run",
            distanceKm = 10,
            price = 150,
            maxParticipants
        });
        response.EnsureSuccessStatusCode();
        return (await ReadJson(response)).GetProperty("categoryId").GetInt32();
    }

    public static async Task<int> Enrol(HttpClient participant, int categoryId)
    {
        var response = await participant.PostAsync($"/api/categories/{categoryId}/enrol", null);
        response.EnsureSuccessStatusCode();
        return (await ReadJson(response)).GetProperty("enrolmentId").GetInt32();
    }
}