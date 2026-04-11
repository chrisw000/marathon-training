using System.Net;
using System.Text.Json;
using Bogus;
using MarathonTraining.Api.IntegrationTests.Support;
using MarathonTraining.Application.Strava;
using MarathonTraining.Domain.Aggregates;
using MarathonTraining.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace MarathonTraining.Api.IntegrationTests.Steps;

[Binding]
public sealed class StravaAuthSteps(StravaAuthContext context)
{
    private static readonly Faker Fake = new();

    // ── Given ────────────────────────────────────────────────────────────────

    [Given("I am authenticated as a valid athlete")]
    public async Task GivenIAmAuthenticatedAsAValidAthlete()
    {
        // Use a GUID-based userId that looks like an Entra External ID object ID.
        var athleteUserId = Guid.NewGuid().ToString();
        context.AthleteUserId = athleteUserId;

        // Seed an AthleteProfile so the command handler can resolve the userId → profileId.
        await using var scope = context.Factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var profile = new AthleteProfile(
            id: Guid.NewGuid(),
            userId: athleteUserId,
            displayName: Fake.Name.FullName(),
            createdAt: DateTimeOffset.UtcNow);

        db.AthleteProfiles.Add(profile);
        await db.SaveChangesAsync();

        // Pre-register a state token in the singleton state service.
        // The callback endpoint reads from the same singleton, so this token will
        // be considered valid when the When step drives the callback request.
        var stateService = context.Factory.Services.GetRequiredService<IStravaOAuthStateService>();
        context.StateToken = stateService.GenerateState(athleteUserId);
    }

    // ── When ─────────────────────────────────────────────────────────────────

    [When("Strava calls the callback endpoint with a valid authorisation code")]
    public async Task WhenStravaCallsCallbackWithValidCode()
    {
        var tokenResponseBody = JsonSerializer.Serialize(new
        {
            access_token = Fake.Random.AlphaNumeric(40),
            refresh_token = Fake.Random.AlphaNumeric(40),
            expires_at = DateTimeOffset.UtcNow.AddHours(6).ToUnixTimeSeconds(),
            athlete = new { id = Fake.Random.Long(1_000_000, 9_999_999) },
        });

        context.WireMockServer!
            .Given(Request.Create().WithPath("/oauth/token").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(tokenResponseBody));

        context.LastResponse = await context.HttpClient!.GetAsync(
            $"/api/strava/callback?code=valid-auth-code&state={Uri.EscapeDataString(context.StateToken!)}");
    }

    [When("Strava calls the callback endpoint with an invalid authorisation code")]
    public async Task WhenStravaCallsCallbackWithInvalidCode()
    {
        var errorResponseBody = JsonSerializer.Serialize(new
        {
            message = "Bad Request",
            errors = new[]
            {
                new { resource = "Application", field = "code", code = "invalid" },
            },
        });

        context.WireMockServer!
            .Given(Request.Create().WithPath("/oauth/token").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(400)
                .WithHeader("Content-Type", "application/json")
                .WithBody(errorResponseBody));

        context.LastResponse = await context.HttpClient!.GetAsync(
            $"/api/strava/callback?code=invalid-auth-code&state={Uri.EscapeDataString(context.StateToken!)}");
    }

    // ── Then ─────────────────────────────────────────────────────────────────

    [Then("the athlete's Strava connection is stored")]
    public async Task ThenAthleteStravaConnectionIsStored()
    {
        // Use a fresh scope so EF's change tracker cannot serve cached data.
        await using var scope = context.Factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var profile = await db.AthleteProfiles
            .FirstOrDefaultAsync(a => a.UserId == context.AthleteUserId);

        profile.Should().NotBeNull("an AthleteProfile was seeded for this scenario");

        var connection = await db.StravaConnections
            .FirstOrDefaultAsync(c => c.AthleteProfileId == profile!.Id);

        connection.Should().NotBeNull(
            "ConnectStravaCommand should have upserted a StravaConnection after the token exchange");

        connection!.AccessToken.Should().NotBeNullOrWhiteSpace();
        connection.RefreshToken.Should().NotBeNullOrWhiteSpace();
        connection.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    // The URL contains "//" which cucumber expressions parse as an empty alternative.
    // Prefixing with "^" switches Reqnroll to regex mode, where "/" is a literal character.
    [Then(@"^the response redirects to http://localhost:5173/strava-connected$")]
    public void ThenResponseRedirectsToStravaConnected()
    {
        context.LastResponse!.StatusCode.Should().Be(HttpStatusCode.Found);
        context.LastResponse.Headers.Location
            .Should().Be(new Uri("http://localhost:5173/strava-connected"));
    }

    [Then("the athlete's Strava connection is not stored")]
    public async Task ThenAthleteStravaConnectionIsNotStored()
    {
        await using var scope = context.Factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var anyConnection = await db.StravaConnections.AnyAsync();

        anyConnection.Should().BeFalse(
            "the token exchange failed so no StravaConnection should have been persisted");
    }

    [Then("the response returns a 400 status")]
    public void ThenResponseReturns400()
    {
        context.LastResponse!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
