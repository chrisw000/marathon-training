using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MarathonTraining.Api.IntegrationTests.Support;
using MarathonTraining.Domain.Aggregates;
using MarathonTraining.Domain.Enums;
using MarathonTraining.Domain.ValueObjects;
using MarathonTraining.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;

namespace MarathonTraining.Api.IntegrationTests.Steps;

[Binding]
public sealed class TrainingLoadSteps(TrainingLoadContext context)
{
    // ── Given ─────────────────────────────────────────────────────────────────

    [Given("I am an authenticated athlete for training load")]
    public async Task GivenIAmAnAuthenticatedAthleteForTrainingLoad()
    {
        context.AthleteUserId = Guid.NewGuid().ToString();
        context.HttpClient!.DefaultRequestHeaders.Add(
            FakeAuthHandler.UserIdHeader,
            context.AthleteUserId);

        await using var scope = context.Factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var profile = new AthleteProfile(
            Guid.NewGuid(),
            context.AthleteUserId,
            "Training Load Athlete",
            DateTimeOffset.UtcNow.AddDays(-90));

        db.AthleteProfiles.Add(profile);
        await db.SaveChangesAsync();
        context.AthleteProfileId = profile.Id;
    }

    [Given("I am not authenticated for training load")]
    public void GivenIAmNotAuthenticatedForTrainingLoad()
    {
        // No auth header — request will be anonymous.
    }

    [Given("the athlete has no training activities")]
    public void GivenTheAthleteHasNoTrainingActivities()
    {
        // Nothing to seed — DB is already clean.
    }

    [Given("the athlete has activities with known TSS values")]
    public async Task GivenTheAthleteHasActivitiesWithKnownTssValues()
    {
        await using var scope = context.Factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Create a training week
        var weekStart = new DateOnly(2025, 12, 29);
        var week = new TrainingWeek(Guid.NewGuid(), context.AthleteProfileId!.Value, weekStart);
        db.TrainingWeeks.Add(week);

        // Add a run with TSS on Jan 5 (within our query range Jan 1–31)
        var activity = new Activity(
            id: Guid.NewGuid(),
            trainingWeekId: week.Id,
            athleteProfileId: context.AthleteProfileId.Value,
            activityType: ActivityType.Run,
            name: "Morning run",
            startedAt: new DateTimeOffset(2026, 1, 5, 8, 0, 0, TimeSpan.Zero),
            durationSeconds: 3600,
            distanceMetres: 10000,
            tssScore: TssScore.Create(75m),
            averageHeartRateBpm: 145,
            maxHeartRateBpm: 175);

        db.Activities.Add(activity);
        await db.SaveChangesAsync();
    }

    [Given("the athlete has a training week with activities")]
    public async Task GivenTheAthleteHasATrainingWeekWithActivities()
    {
        await using var scope = context.Factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var weekStart = new DateOnly(2026, 1, 6);
        var week = new TrainingWeek(Guid.NewGuid(), context.AthleteProfileId!.Value, weekStart);
        db.TrainingWeeks.Add(week);

        for (int i = 0; i < 3; i++)
        {
            db.Activities.Add(new Activity(
                id: Guid.NewGuid(),
                trainingWeekId: week.Id,
                athleteProfileId: context.AthleteProfileId.Value,
                activityType: ActivityType.Run,
                name: $"Run {i + 1}",
                startedAt: new DateTimeOffset(2026, 1, 6 + i, 7, 0, 0, TimeSpan.Zero),
                durationSeconds: 3600,
                distanceMetres: 10000,
                tssScore: TssScore.Create(80m),
                averageHeartRateBpm: 145,
                maxHeartRateBpm: 175));
        }

        db.Activities.Add(new Activity(
            id: Guid.NewGuid(),
            trainingWeekId: week.Id,
            athleteProfileId: context.AthleteProfileId.Value,
            activityType: ActivityType.Ride,
            name: "Sunday ride",
            startedAt: new DateTimeOffset(2026, 1, 10, 9, 0, 0, TimeSpan.Zero),
            durationSeconds: 5400,
            distanceMetres: 40000,
            tssScore: TssScore.Create(100m),
            normalisedPowerWatts: 200));

        await db.SaveChangesAsync();
    }

    // ── When ──────────────────────────────────────────────────────────────────

    [When(@"^I request GET /api/training/load\?from=2026-01-01&to=2026-01-31$")]
    public async Task WhenIRequestTrainingLoad()
    {
        context.LastResponse = await context.HttpClient!.GetAsync(
            "/api/training/load?from=2026-01-01&to=2026-01-31");
    }

    [When(@"^I request GET /api/training/load\?from=invalid&to=2026-01-31$")]
    public async Task WhenIRequestTrainingLoadWithInvalidDate()
    {
        context.LastResponse = await context.HttpClient!.GetAsync(
            "/api/training/load?from=invalid&to=2026-01-31");
    }

    [When(@"^I request GET /api/training/load$")]
    public async Task WhenIRequestTrainingLoadUnauthenticated()
    {
        context.LastResponse = await context.HttpClient!.GetAsync("/api/training/load");
    }

    [When(@"^I request GET /api/training/week/2026-01-06$")]
    public async Task WhenIRequestWeekSummary()
    {
        context.LastResponse = await context.HttpClient!.GetAsync("/api/training/week/2026-01-06");
    }

    [When(@"^I POST to /api/training/recalculate$")]
    public async Task WhenIPostRecalculate()
    {
        context.LastResponse = await context.HttpClient!.PostAsync(
            "/api/training/recalculate", content: null);
    }

    // ── Then ──────────────────────────────────────────────────────────────────

    [Then("the training load response status is {int}")]
    public void ThenTheResponseStatusIs(int statusCode)
    {
        ((int)context.LastResponse!.StatusCode).Should().Be(statusCode);
    }

    [Then("the training load response is an empty array")]
    public async Task ThenTheResponseIsEmptyArray()
    {
        var json = await context.LastResponse!.Content.ReadAsStringAsync();
        var array = JsonDocument.Parse(json).RootElement;
        array.GetArrayLength().Should().Be(0, "no activities means no training load data");
    }

    [Then("the response contains ATL CTL and TSB values")]
    public async Task ThenResponseContainsAtlCtlTsb()
    {
        var json = await context.LastResponse!.Content.ReadAsStringAsync();
        var array = JsonDocument.Parse(json).RootElement;

        array.GetArrayLength().Should().BeGreaterThan(0, "seeded activities should produce load entries");

        var first = array[0];
        first.TryGetProperty("atl", out _).Should().BeTrue("response should contain 'atl' field");
        first.TryGetProperty("ctl", out _).Should().BeTrue("response should contain 'ctl' field");
        first.TryGetProperty("tsb", out _).Should().BeTrue("response should contain 'tsb' field");
    }

    [Then("the response contains week summary fields")]
    public async Task ThenResponseContainsWeekSummaryFields()
    {
        var json = await context.LastResponse!.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json).RootElement;

        doc.TryGetProperty("totalTss", out _).Should().BeTrue();
        doc.TryGetProperty("runCount", out _).Should().BeTrue();
        doc.TryGetProperty("rideCount", out _).Should().BeTrue();
        doc.TryGetProperty("recommendation", out _).Should().BeTrue();
    }

    [Then("the response contains a recalculated count")]
    public async Task ThenResponseContainsRecalculatedCount()
    {
        var json = await context.LastResponse!.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json).RootElement;

        doc.TryGetProperty("recalculated", out _).Should().BeTrue();
    }
}
