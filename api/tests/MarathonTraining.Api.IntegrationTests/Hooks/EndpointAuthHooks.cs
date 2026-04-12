using MarathonTraining.Api.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Reqnroll;

namespace MarathonTraining.Api.IntegrationTests.Hooks;

[Binding]
public sealed class EndpointAuthHooks(EndpointAuthContext context)
{
    [BeforeScenario(Order = 30)]
    public void BeforeScenario()
    {
        // No WireMock and no DB wipe — the auth middleware rejects the request
        // before any handler or DB access takes place.
        context.Factory = new ApiWebApplicationFactory(
            stravaBaseUrl: "http://localhost:9999",
            testConnectionString: ContainerHooks.ConnectionString);

        context.HttpClient = context.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        // Deliberately do NOT set X-Test-User-Id — the request must be anonymous.
    }

    [AfterScenario(Order = 30)]
    public void AfterScenario()
    {
        context.LastResponse?.Dispose();
        context.HttpClient?.Dispose();
        context.Factory?.Dispose();
    }
}
