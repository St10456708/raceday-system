using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RaceDayApi.Data;
using Xunit;
using static RaceDayApi.Tests.TestHelpers;

namespace RaceDayApi.Tests;

public class AuthTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AuthTests(ApiFactory factory)
    {
        _factory = factory;
    }

    // ---------- Registration ----------

    [Theory]
    [InlineData("Participant")]
    [InlineData("Organiser")]
    public async Task Register_WithValidRole_Returns201(string role)
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "New User",
            email = UniqueEmail(),
            password = Password,
            role
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await ReadJson(response);
        Assert.Equal(role, json.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var client = _factory.CreateClient();
        var body = new { fullName = "Dup User", email = UniqueEmail(), password = Password, role = "Participant" };

        var first = await client.PostAsJsonAsync("/api/auth/register", body);
        var second = await client.PostAsJsonAsync("/api/auth/register", body);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Hacker",
            email = UniqueEmail(),
            password = Password,
            role = "Admin"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_MissingFields_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "",
            email = "not-an-email",
            password = "123",
            role = "Participant"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_StoresPasswordHashed_NotPlainText()
    {
        var client = _factory.CreateClient();
        var email = UniqueEmail();

        await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Hash Test",
            email,
            password = Password,
            role = "Participant"
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = db.Users.Single(u => u.Email == email);

        Assert.NotEqual(Password, user.PasswordHash);
        Assert.StartsWith("$2", user.PasswordHash); // BCrypt hashes start with $2
    }

    // ---------- Login ----------

    [Fact]
    public async Task Login_WithCorrectCredentials_Returns200AndRole()
    {
        var client = _factory.CreateClient();
        var email = UniqueEmail();
        await client.PostAsJsonAsync("/api/auth/register", new { fullName = "Login Test", email, password = Password, role = "Organiser" });

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJson(response);
        Assert.Equal("Organiser", json.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = _factory.CreateClient();
        var email = UniqueEmail();
        await client.PostAsJsonAsync("/api/auth/register", new { fullName = "Login Test", email, password = Password, role = "Participant" });

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPassword1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = UniqueEmail(), password = Password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- Session / protected endpoints ----------

    [Fact]
    public async Task Profile_WithoutLogin_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Profile_AfterLogin_ReturnsOwnProfile()
    {
        var (client, userId, email) = await LoginAsNewUser(_factory, "Participant");

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJson(response);
        Assert.Equal(userId, json.GetProperty("userId").GetInt32());
        Assert.Equal(email, json.GetProperty("email").GetString());
        Assert.Equal("Participant", json.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Profile_AfterLogout_Returns401()
    {
        var (client, _, _) = await LoginAsNewUser(_factory, "Participant");

        await client.PostAsync("/api/auth/logout", null);
        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("Participant")]
    [InlineData("Organiser")]
    public async Task Profile_Update_ChangesOwnDetails(string role)
    {
        var (client, _, _) = await LoginAsNewUser(_factory, role);

        var update = await client.PutAsJsonAsync("/api/users/me", new { fullName = "Updated Name", phoneNumber = "0725550000" });
        var profile = await ReadJson(await client.GetAsync("/api/users/me"));

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("Updated Name", profile.GetProperty("fullName").GetString());
        Assert.Equal("0725550000", profile.GetProperty("phoneNumber").GetString());
    }
}