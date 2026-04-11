using MarathonTraining.Application.Strava;
using MarathonTraining.Infrastructure.Persistence;
using MarathonTraining.Infrastructure.Strava;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MarathonTraining.Api.IntegrationTests.Support;

/// <summary>
/// Hosts the API in-process for integration tests. Replaces three registrations:
/// <list type="bullet">
///   <item>
///     <see cref="AppDbContext"/> — redirected to the Testcontainer SQL Server.
///   </item>
///   <item>
///     <see cref="IStravaTokenService"/> HttpClient — base address redirected to the
///     WireMock server so Strava token exchange is intercepted without real network calls.
///   </item>
///   <item>
///     JWT Bearer authentication — replaced with <see cref="FakeAuthHandler"/> so tests
///     can authenticate by setting the <c>X-Test-User-Id</c> request header rather than
///     generating real JWTs. Anonymous endpoints are unaffected.
///   </item>
/// </list>
/// </summary>
public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _stravaBaseUrl;
    private readonly string _testConnectionString;

    /// <param name="stravaBaseUrl">
    ///   Base URL of the WireMock server that will handle <c>POST /oauth/token</c> calls.
    ///   Pass any valid URL for tests that do not exercise the Strava token exchange.
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
            var dbOptionsDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbOptionsDescriptor is not null)
                services.Remove(dbOptionsDescriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(_testConnectionString));

            // ── Redirect Strava HTTP client to WireMock ───────────────────────
            var stravaDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IStravaTokenService));
            if (stravaDescriptor is not null)
                services.Remove(stravaDescriptor);

            services.AddHttpClient<IStravaTokenService, StravaTokenService>(client =>
                client.BaseAddress = new Uri(_stravaBaseUrl));

            // ── Replace JWT Bearer auth with FakeAuthHandler ──────────────────
            // Tests authenticate by setting X-Test-User-Id on the request.
            // Anonymous endpoints (e.g. /api/strava/callback) are unaffected.
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = FakeAuthHandler.SchemeName;
                options.DefaultChallengeScheme = FakeAuthHandler.SchemeName;
            })
            .AddScheme<AuthenticationSchemeOptions, FakeAuthHandler>(
                FakeAuthHandler.SchemeName, _ => { });
        });
    }
}
