using System.Net;
using System.Text.Json;
using Bogus;
using MarathonTraining.Api.IntegrationTests.Support;
using MarathonTraining.Domain.Aggregates;
using MarathonTraining.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;

namespace MarathonTraining.Api.IntegrationTests.Steps;

[Binding]
public sealed class StravaConnectionSteps(StravaConnectionContext context)
{
    private static readonly Faker Fake = new();

    // ── Given ────────────────────────────────────────────────────────────────

    [Given("I am an authenticated athlete with no Strava connection")]
    public async Task GivenIAmAnAuthenticatedAthleteWithNoConnection()
    {
        await SeedAthleteAndAuthenticateAsync();
    }

    [Given("I am an authenticated athlete with a connected Strava account")]
    public async Task GivenIAmAnAuthenticatedAthleteWithAConnectedStravaAccount()
    {
        var profile = await SeedAthleteAndAuthenticateAsync();

        await using var scope = context.Factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.StravaConnections.Add(new StravaConnection(
            athleteProfileId: profile.Id,
            stravaAthleteId: Fake.Random.Long(1_000_000, 9_999_999),
            accessToken: Fake.Random.AlphaNumeric(40),
            refreshToken: Fake.Random.AlphaNumeric(40),
            expiresAt: DateTimeOffset.UtcNow.AddHours(6)));

        await db.SaveChangesAsync();
    }

    // ── When ─────────────────────────────────────────────────────────────────

    [When("I request the Strava connection status")]
    public async Task WhenIRequestTheStravaConnectionStatus()
    {
        context.LastResponse = await context.HttpClient!.GetAsync("/api/strava/status");
    }

    [When("I send a request to disconnect Strava")]
    public async Task WhenISendARequestToDisconnectStrava()
    {
        context.LastResponse = await context.HttpClient!.DeleteAsync("/api/strava/disconnect");
    }

    [When("I request the Strava authorisation URL")]
    public async Task WhenIRequestTheStravaAuthorisationUrl()
    {
        context.LastResponse = await context.HttpClient!.GetAsync("/api/strava/authorise");
    }

    // ── Then ─────────────────────────────────────────────────────────────────

    [Then("the status response indicates Strava is connected")]
    public async Task ThenStatusResponseIndicatesStravaIsConnected()
    {
        context.LastResponse!.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await ParseResponseBodyAsync();
        body.GetProperty("isConnected").GetBoolean().Should().BeTrue();
        body.GetProperty("stravaAthleteId").GetInt64().Should().BeGreaterThan(0);
    }

    [Then("the status response indicates Strava is not connected")]
    public async Task ThenStatusResponseIndicatesStravaIsNotConnected()
    {
        context.LastResponse!.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await ParseResponseBodyAsync();
        body.GetProperty("isConnected").GetBoolean().Should().BeFalse();
    }

    [Then("the disconnect response returns a 204 status")]
    public void ThenDisconnectResponseReturns204()
    {
        context.LastResponse!.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Then("the disconnect response returns a 400 status")]
    public void ThenDisconnectResponseReturns400()
    {
        context.LastResponse!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Then("the Strava connection no longer exists in the database")]
    public async Task ThenStravaConnectionNoLongerExistsInDatabase()
    {
        await using var scope = context.Factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var profile = await db.AthleteProfiles
            .FirstOrDefaultAsync(a => a.UserId == context.AthleteUserId);

        profile.Should().NotBeNull();

        var connection = await db.StravaConnections
            .FirstOrDefaultAsync(c => c.AthleteProfileId == profile!.Id);

        connection.Should().BeNull(
            "DisconnectStravaCommand should have deleted the StravaConnection");
    }

    [Then("the response contains a valid Strava OAuth URL")]
    public async Task ThenResponseContainsValidStravaOAuthUrl()
    {
        context.LastResponse!.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await ParseResponseBodyAsync();
        var url = body.GetProperty("url").GetString();

        url.Should().NotBeNullOrWhiteSpace();
        url.Should().Contain("strava.com/oauth/authorize");
        url.Should().Contain("response_type=code");
        url.Should().Contain("scope=activity");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<AthleteProfile> SeedAthleteAndAuthenticateAsync()
    {
        var athleteUserId = Guid.NewGuid().ToString();
        context.AthleteUserId = athleteUserId;

        await using var scope = context.Factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var profile = new AthleteProfile(
            id: Guid.NewGuid(),
            userId: athleteUserId,
            displayName: Fake.Name.FullName(),
            createdAt: DateTimeOffset.UtcNow);

        db.AthleteProfiles.Add(profile);
        await db.SaveChangesAsync();

        context.HttpClient!.DefaultRequestHeaders.Remove(FakeAuthHandler.UserIdHeader);
        context.HttpClient.DefaultRequestHeaders.Add(FakeAuthHandler.UserIdHeader, athleteUserId);

        return profile;
    }

    private async Task<JsonElement> ParseResponseBodyAsync()
    {
        var json = await context.LastResponse!.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement;
    }
}
