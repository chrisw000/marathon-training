using MarathonTraining.Api.IntegrationTests.Support;
using MarathonTraining.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using WireMock.Server;

namespace MarathonTraining.Api.IntegrationTests.Hooks;

[Binding]
public sealed class ActivitiesHooks(ActivitiesContext context)
{
    [BeforeScenario(Order = 10)]
    public async Task BeforeScenario()
    {
        // Always start WireMock — sync scenarios stub the Strava activities endpoint;
        // manual/list scenarios leave it idle. Overhead is negligible.
        context.WireMockServer = WireMockServer.Start();

        context.Factory = new ApiWebApplicationFactory(
            stravaBaseUrl: context.WireMockServer.Url!,
            testConnectionString: ContainerHooks.ConnectionString);

        context.HttpClient = context.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        // Wipe DB in FK-safe order — each scenario starts from a clean slate.
        await using var scope = context.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Activities.ExecuteDeleteAsync();
        await db.TrainingWeeks.ExecuteDeleteAsync();
        await db.StravaConnections.ExecuteDeleteAsync();
        await db.AthleteProfiles.ExecuteDeleteAsync();
    }

    [AfterScenario(Order = 10)]
    public void AfterScenario()
    {
        context.LastResponse?.Dispose();
        context.HttpClient?.Dispose();
        context.Factory?.Dispose();
        context.WireMockServer?.Stop();
    }
}
