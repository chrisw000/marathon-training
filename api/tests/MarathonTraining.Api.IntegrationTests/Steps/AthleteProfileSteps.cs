using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MarathonTraining.Api.IntegrationTests.Support;
using MarathonTraining.Domain.Aggregates;
using MarathonTraining.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;

namespace MarathonTraining.Api.IntegrationTests.Steps;

[Binding]
public sealed class AthleteProfileSteps(AthleteProfileContext context)
{
    // ── Given ────────────────────────────────────────────────────────────────

    [Given("I am a new authenticated user")]
    public void GivenIAmANewAuthenticatedUser()
    {
        context.AthleteUserId = Guid.NewGuid().ToString();

        // Authenticate by setting the test header — FakeAuthHandler turns this into
        // a ClaimsPrincipal with an "oid" claim, which GetObjectId() reads.
        context.HttpClient!.DefaultRequestHeaders.Add(
            FakeAuthHandler.UserIdHeader,
            context.AthleteUserId);
    }

    [Given("my athlete profile already exists")]
    public async Task GivenMyAthleteProfileAlreadyExists()
    {
        await using var scope = context.Factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.AthleteProfiles.Add(new AthleteProfile(
            Guid.NewGuid(),
            context.AthleteUserId!,
            "Existing Athlete",
            DateTimeOffset.UtcNow.AddDays(-1)));

        await db.SaveChangesAsync();
    }

    [Given("I am an authenticated athlete with an existing profile")]
    public async Task GivenIAmAnAuthenticatedAthleteWithAnExistingProfile()
    {
        context.AthleteUserId = Guid.NewGuid().ToString();
        context.AthleteDisplayName = "Test Athlete";

        context.HttpClient!.DefaultRequestHeaders.Add(
            FakeAuthHandler.UserIdHeader,
            context.AthleteUserId);

        await using var scope = context.Factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.AthleteProfiles.Add(new AthleteProfile(
            Guid.NewGuid(),
            context.AthleteUserId,
            context.AthleteDisplayName,
            DateTimeOffset.UtcNow.AddDays(-10)));

        await db.SaveChangesAsync();
    }

    [Given("I am not authenticated")]
    public void GivenIAmNotAuthenticated()
    {
        // Deliberately omit X-Test-User-Id — request will be anonymous
    }

    // ── When ─────────────────────────────────────────────────────────────────

    [When("I call the ensure profile endpoint")]
    public async Task WhenICallTheEnsureProfileEndpoint()
    {
        context.LastResponse = await context.HttpClient!.PostAsync("/api/profile", content: null);
    }

    [When("I request the athlete profile endpoint")]
    public async Task WhenIRequestGetAthleteProfile()
    {
        context.LastResponse = await context.HttpClient!.GetAsync("/api/athlete/profile");
    }

    [When("I submit a valid physiology update")]
    public async Task WhenISubmitAValidPhysiologyUpdate()
    {
        context.LastResponse = await context.HttpClient!.PatchAsJsonAsync(
            "/api/athlete/physiology",
            new { restingHr = 55, maxHr = 185, thresholdHr = 165, ftpWatts = 260 });
    }

    [When("I submit physiology with RestingHr greater than MaxHr")]
    public async Task WhenISubmitPhysiologyWithInvalidHrZones()
    {
        context.LastResponse = await context.HttpClient!.PatchAsJsonAsync(
            "/api/athlete/physiology",
            new { restingHr = 200, maxHr = 185, thresholdHr = 165, ftpWatts = 260 });
    }

    [When("I update the training phase to {string}")]
    public async Task WhenIUpdateTheTrainingPhaseTo(string phase)
    {
        context.LastResponse = await context.HttpClient!.PatchAsJsonAsync(
            "/api/athlete/phase",
            new { phase });
    }

    // ── Then ─────────────────────────────────────────────────────────────────

    [Then("the response indicates the profile was created")]
    public async Task ThenResponseIndicatesProfileWasCreated()
    {
        context.LastResponse!.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await ParseResponseBodyAsync();
        body.GetProperty("created").GetBoolean().Should().BeTrue();
    }

    [Then("the response indicates the profile already existed")]
    public async Task ThenResponseIndicatesProfileAlreadyExisted()
    {
        context.LastResponse!.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await ParseResponseBodyAsync();
        body.GetProperty("created").GetBoolean().Should().BeFalse();
    }

    [Then("the athlete profile exists in the database")]
    public async Task ThenAthleteProfileExistsInDatabase()
    {
        await using var scope = context.Factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var profile = await db.AthleteProfiles
            .FirstOrDefaultAsync(p => p.UserId == context.AthleteUserId);

        profile.Should().NotBeNull("the endpoint should have created a profile for this user");
    }

    [Then("there is still only one athlete profile in the database")]
    public async Task ThenThereIsStillOnlyOneAthleteProfile()
    {
        await using var scope = context.Factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var count = await db.AthleteProfiles
            .CountAsync(p => p.UserId == context.AthleteUserId);

        count.Should().Be(1, "calling the endpoint twice should not create duplicate profiles");
    }

    [Then("the response status is {int}")]
    public void ThenTheResponseStatusIs(int statusCode)
    {
        ((int)context.LastResponse!.StatusCode).Should().Be(statusCode);
    }

    [Then("the response contains the athlete display name")]
    public async Task ThenTheResponseContainsTheAthleteDisplayName()
    {
        var body = await ParseResponseBodyAsync();
        body.GetProperty("displayName").GetString()
            .Should().Be(context.AthleteDisplayName);
    }

    [Then("the response contains currentPhase {string}")]
    public async Task ThenTheResponseContainsCurrentPhase(string expectedPhase)
    {
        var body = await ParseResponseBodyAsync();
        body.GetProperty("currentPhase").GetString()
            .Should().Be(expectedPhase);
    }

    [Then("the response reflects the updated values")]
    public async Task ThenTheResponseReflectsTheUpdatedValues()
    {
        var body = await ParseResponseBodyAsync();
        body.GetProperty("restingHr").GetInt32().Should().Be(55);
        body.GetProperty("maxHr").GetInt32().Should().Be(185);
        body.GetProperty("thresholdHr").GetInt32().Should().Be(165);
        body.GetProperty("ftpWatts").GetInt32().Should().Be(260);
    }

    [Then("the response is a ProblemDetails with validation errors")]
    public async Task ThenTheResponseIsAProblemDetailsWithValidationErrors()
    {
        var body = await ParseResponseBodyAsync();
        body.TryGetProperty("errors", out _).Should().BeTrue(
            "a validation problem response should contain an 'errors' property");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<JsonElement> ParseResponseBodyAsync()
    {
        var json = await context.LastResponse!.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement;
    }
}
