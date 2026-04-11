using MarathonTraining.Api.IntegrationTests.Support;
using MarathonTraining.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;

namespace MarathonTraining.Api.IntegrationTests.Hooks;

[Binding]
public sealed class AthleteProfileHooks(AthleteProfileContext context)
{
    [BeforeScenario(Order = 10)]
    public async Task BeforeScenario()
    {
        // No WireMock needed — profile tests do not exercise the Strava token exchange.
        context.Factory = new ApiWebApplicationFactory(
            stravaBaseUrl: "http://localhost:9999",
            testConnectionString: ContainerHooks.ConnectionString);

        // Disable auto-redirect so any 302 responses remain visible in assertions.
        context.HttpClient = context.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        // Wipe all rows so each scenario starts from a known empty state.
        await using var scope = context.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.StravaConnections.ExecuteDeleteAsync();
        await db.AthleteProfiles.ExecuteDeleteAsync();
    }

    [AfterScenario(Order = 10)]
    public void AfterScenario()
    {
        context.LastResponse?.Dispose();
        context.HttpClient?.Dispose();
        context.Factory?.Dispose();
    }
}
