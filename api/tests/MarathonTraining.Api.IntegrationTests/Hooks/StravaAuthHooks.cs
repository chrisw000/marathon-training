using MarathonTraining.Api.IntegrationTests.Support;
using MarathonTraining.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using WireMock.Server;

namespace MarathonTraining.Api.IntegrationTests.Hooks;

[Binding]
public sealed class StravaAuthHooks(StravaAuthContext context)
{
    [BeforeScenario]
    public async Task BeforeScenario()
    {
        // Start WireMock first — its URL is needed by the factory.
        context.WireMockServer = WireMockServer.Start();

        // Build the in-process host.
        // The SQL Server connection string comes from the shared Testcontainer started in
        // ContainerHooks.StartSqlServerAsync, so no Docker spin-up cost here.
        context.Factory = new ApiWebApplicationFactory(
            stravaBaseUrl: context.WireMockServer.Url!,
            testConnectionString: ContainerHooks.ConnectionString);

        // Disable auto-redirect so 302 responses are visible in assertions.
        context.HttpClient = context.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        // Wipe all rows so each scenario starts from a known empty state.
        // Schema already exists — ContainerHooks.StartSqlServerAsync called EnsureCreated once
        // before any scenario ran.
        await using var scope = context.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Delete in FK-safe order: child rows first.
        await db.StravaConnections.ExecuteDeleteAsync();
        await db.AthleteProfiles.ExecuteDeleteAsync();
    }

    [AfterScenario]
    public void AfterScenario()
    {
        context.LastResponse?.Dispose();
        context.HttpClient?.Dispose();
        context.Factory?.Dispose();
        context.WireMockServer?.Stop();
    }
}
