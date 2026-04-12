using MarathonTraining.Api.IntegrationTests.Support;
using MarathonTraining.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;

namespace MarathonTraining.Api.IntegrationTests.Hooks;

[Binding]
public sealed class StravaConnectionHooks(StravaConnectionContext context)
{
    [BeforeScenario(Order = 20)]
    public async Task BeforeScenario()
    {
        // No WireMock needed — status, disconnect, and authorise do not call the Strava API.
        context.Factory = new ApiWebApplicationFactory(
            stravaBaseUrl: "http://localhost:9999",
            testConnectionString: ContainerHooks.ConnectionString);

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

    [AfterScenario(Order = 20)]
    public void AfterScenario()
    {
        context.LastResponse?.Dispose();
        context.HttpClient?.Dispose();
        context.Factory?.Dispose();
    }
}
