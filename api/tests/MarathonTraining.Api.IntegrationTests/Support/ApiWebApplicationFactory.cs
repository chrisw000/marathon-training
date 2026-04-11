using MarathonTraining.Application.Strava;
using MarathonTraining.Infrastructure.Persistence;
using MarathonTraining.Infrastructure.Strava;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MarathonTraining.Api.IntegrationTests.Support;

/// <summary>
/// Hosts the API in-process for integration tests. Replaces two registrations:
/// <list type="bullet">
///   <item>
///     <see cref="AppDbContext"/> — redirected to the Testcontainer SQL Server so tests
///     run against a real, isolated database without touching production data.
///   </item>
///   <item>
///     <see cref="IStravaTokenService"/> HttpClient — base address redirected to the
///     WireMock server so Strava token exchange is intercepted without real network calls.
///   </item>
/// </list>
/// </summary>
public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _stravaBaseUrl;
    private readonly string _testConnectionString;

    /// <param name="stravaBaseUrl">
    ///   Base URL of the WireMock server that will handle <c>POST /oauth/token</c> calls.
    ///   A trailing slash is added if absent.
    /// </param>
    /// <param name="testConnectionString">
    ///   Connection string to the Testcontainer SQL Server database.
    ///   Sourced from <see cref="ContainerHooks.ConnectionString"/>.
    /// </param>
    public ApiWebApplicationFactory(string stravaBaseUrl, string testConnectionString)
    {
        _stravaBaseUrl = stravaBaseUrl.TrimEnd('/') + "/";
        _testConnectionString = testConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // ── Replace AppDbContext ──────────────────────────────────────────
            // Remove the options singleton that carries the production connection string.
            var dbOptionsDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbOptionsDescriptor is not null)
                services.Remove(dbOptionsDescriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(_testConnectionString));

            // ── Redirect Strava HTTP client to WireMock ───────────────────────
            // Remove the typed-client binding so a new one can be registered with the
            // WireMock base address.  The IHttpClientFactory options for this named
            // client accumulate both the original base-address action (Program.cs) and
            // the one below; because actions run in registration order the last write wins,
            // so the WireMock URL takes effect.
            var stravaDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IStravaTokenService));
            if (stravaDescriptor is not null)
                services.Remove(stravaDescriptor);

            services.AddHttpClient<IStravaTokenService, StravaTokenService>(client =>
                client.BaseAddress = new Uri(_stravaBaseUrl));
        });
    }
}
