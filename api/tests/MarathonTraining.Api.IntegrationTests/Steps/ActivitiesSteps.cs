using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bogus;
using MarathonTraining.Api.IntegrationTests.Support;
using MarathonTraining.Domain.Aggregates;
using MarathonTraining.Domain.Enums;
using MarathonTraining.Domain.ValueObjects;
using MarathonTraining.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace MarathonTraining.Api.IntegrationTests.Steps;

[Binding]
public sealed class ActivitiesSteps(ActivitiesContext context)
{
    private static readonly Faker Fake = new();

    // ── Given ─────────────────────────────────────────────────────────────────

    [Given("I am an authenticated athlete for activities")]
    public async Task GivenIAmAnAuthenticatedAthleteForActivities()
    {
        await SeedAthleteAndAuthenticateAsync();
    }

    [Given("I am an authenticated athlete with a Strava connection for activities")]
    public async Task GivenIAmAnAuthenticatedAthleteWithStravaConnection()
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

    [Given("I am an authenticated athlete without a Strava connection for activities")]
    public async Task GivenIAmAnAuthenticatedAthleteWithoutStravaConnection()
    {
        // Profile exists but no StravaConnection row.
        await SeedAthleteAndAuthenticateAsync();
    }

    [Given("Strava returns {int} new activity for the athlete")]
    [Given("Strava returns {int} activities for the athlete")]
    public void GivenStravaReturnsNActivities(int count)
    {
        var activities = Enumerable.Range(0, count).Select(_ => new
        {
            id = Fake.Random.Long(1_000_000, 999_999_999),
            name = Fake.Lorem.Word() + " Run",
            sport_type = "Run",
            type = "Run",
            start_date = DateTimeOffset.UtcNow.AddDays(-1).ToString("O"),
            elapsed_time = 3600,
            moving_time = 3600,
            distance = 10000.0,
            average_speed = 2.78,
            average_heartrate = 145,
            max_heartrate = 175,
            has_heartrate = true,
            weighted_average_watts = (int?)null,
            device_watts = false,
        }).ToArray();

        var activitiesBody = JsonSerializer.Serialize(activities);

        // Use InScenario so page 1 returns activities and all subsequent pages return empty.
        // Without this the sync handler loops forever because WireMock always returns the
        // same 1 activity, which gets skipped on page 2+ (already in DB) but never results
        // in an empty response that would break the loop.
        context.WireMockServer!
            .Given(Request.Create()
                .WithPath("/api/v3/athlete/activities")
                .UsingGet())
            .InScenario("strava-sync")
            .WillSetStateTo("done")
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(activitiesBody));

        context.WireMockServer!
            .Given(Request.Create()
                .WithPath("/api/v3/athlete/activities")
                .UsingGet())
            .InScenario("strava-sync")
            .WhenStateIs("done")
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("[]"));
    }

    [Given("the athlete has {int} seeded activities")]
    public async Task GivenTheAthleteHasNSeedActivities(int count)
    {
        await using var scope = context.Factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var profile = await db.AthleteProfiles
            .FirstAsync(a => a.UserId == context.AthleteUserId);

        var weekStart = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-6));
        // Align to Monday
        var dow = (int)weekStart.DayOfWeek;
        weekStart = weekStart.AddDays(dow == 0 ? -6 : -(dow - 1));

        var week = new TrainingWeek(Guid.NewGuid(), profile.Id, weekStart);
        db.TrainingWeeks.Add(week);

        for (var i = 0; i < count; i++)
        {
            db.Activities.Add(new Activity(
                id: Guid.NewGuid(),
                trainingWeekId: week.Id,
                athleteProfileId: profile.Id,
                activityType: ActivityType.Run,
                name: $"Run {i + 1}",
                startedAt: DateTimeOffset.UtcNow.AddDays(-i),
                durationSeconds: 3600,
                distanceMetres: 10000,
                tssScore: TssScore.Create(60m),
                averageHeartRateBpm: 145,
                maxHeartRateBpm: 175));
        }

        await db.SaveChangesAsync();
    }

    // ── When ──────────────────────────────────────────────────────────────────

    [When(@"^I POST to /api/activities/sync$")]
    public async Task WhenIPostToSync()
    {
        context.LastResponse = await context.HttpClient!.PostAsync(
            "/api/activities/sync", content: null);
    }

    [When("I POST a manual strength activity with duration {int} and RPE {int}")]
    public async Task WhenIPostManualActivity(int durationMinutes, int rpe)
    {
        var body = new
        {
            name = "Morning Weights",
            activityType = 2,   // ActivityType.Strength — sent as int because no JsonStringEnumConverter is configured
            startedAt = DateTimeOffset.UtcNow,
            durationMinutes,
            rpe,
            notes = (string?)null,
        };

        context.LastResponse = await context.HttpClient!.PostAsJsonAsync(
            "/api/activities/manual", body);
    }

    [When(@"^I request GET /api/activities$")]
    public async Task WhenIRequestGetActivities()
    {
        context.LastResponse = await context.HttpClient!.GetAsync("/api/activities");
    }

    [When(@"^I request GET /api/activities\?type=Run$")]
    public async Task WhenIRequestGetActivitiesFilteredByRun()
    {
        context.LastResponse = await context.HttpClient!.GetAsync("/api/activities?type=Run");
    }

    // ── Then ──────────────────────────────────────────────────────────────────

    [Then("the activities response status is {int}")]
    public void ThenActivitiesResponseStatusIs(int statusCode)
    {
        ((int)context.LastResponse!.StatusCode).Should().Be(statusCode);
    }

    [Then("the response contains activitiesSynced of {int}")]
    public async Task ThenResponseContainsActivitiesSynced(int expected)
    {
        var json = await context.LastResponse!.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json).RootElement;

        doc.TryGetProperty("activitiesSynced", out var prop)
            .Should().BeTrue("response should contain 'activitiesSynced'");

        prop.GetInt32().Should().Be(expected);
    }

    [Then("the response contains a valid activity ID")]
    public async Task ThenResponseContainsValidActivityId()
    {
        var json = await context.LastResponse!.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json).RootElement;

        doc.TryGetProperty("activityId", out var idProp)
            .Should().BeTrue("response should contain 'activityId'");

        Guid.TryParse(idProp.GetString(), out var id).Should().BeTrue();
        id.Should().NotBeEmpty();
    }

    [Then("the response contains {int} activities")]
    public async Task ThenResponseContainsNActivities(int expectedCount)
    {
        var json = await context.LastResponse!.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json).RootElement;

        doc.TryGetProperty("items", out var items)
            .Should().BeTrue("response should contain 'items' array");

        items.GetArrayLength().Should().Be(expectedCount);
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
}
