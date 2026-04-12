using MarathonTraining.Api.IntegrationTests.Support;
using MarathonTraining.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;

namespace MarathonTraining.Api.IntegrationTests.Hooks;

[Binding]
public sealed class TrainingLoadHooks(TrainingLoadContext context)
{
    [BeforeScenario(Order = 10)]
    public async Task BeforeScenario()
    {
        context.Factory = new ApiWebApplicationFactory(
            stravaBaseUrl: "http://localhost:9999",
            testConnectionString: ContainerHooks.ConnectionString);

        context.HttpClient = context.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

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
    }
}
