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

    // ── When ─────────────────────────────────────────────────────────────────

    [When("I call the ensure profile endpoint")]
    public async Task WhenICallTheEnsureProfileEndpoint()
    {
        context.LastResponse = await context.HttpClient!.PostAsync("/api/profile", content: null);
    }

    // ── Then ─────────────────────────────────────────────────────────────────

    [Then("the response indicates the profile was created")]
    public async Task ThenResponseIndicatesProfileWasCreated()
    {
        context.LastResponse!.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var body = await ParseResponseBodyAsync();
        body.GetProperty("created").GetBoolean().Should().BeTrue();
    }

    [Then("the response indicates the profile already existed")]
    public async Task ThenResponseIndicatesProfileAlreadyExisted()
    {
        context.LastResponse!.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

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

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<JsonElement> ParseResponseBodyAsync()
    {
        var json = await context.LastResponse!.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement;
    }
}
