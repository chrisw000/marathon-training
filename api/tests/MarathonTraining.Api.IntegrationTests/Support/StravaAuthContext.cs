using WireMock.Server;

namespace MarathonTraining.Api.IntegrationTests.Support;

/// <summary>
/// Holds all state that flows between the Hooks and Step classes within a single Strava auth scenario.
/// Reqnroll creates one instance per scenario and injects it wherever it is declared as a constructor parameter.
/// </summary>
public sealed class StravaAuthContext
{
    /// <summary>The WireMock server that impersonates https://www.strava.com for this scenario.</summary>
    public WireMockServer? WireMockServer { get; set; }

    /// <summary>The in-process test host, configured to point at WireMock and a test database.</summary>
    public ApiWebApplicationFactory? Factory { get; set; }

    /// <summary>The HTTP client for driving the API. Auto-redirect is disabled so 302 responses are observable.</summary>
    public HttpClient? HttpClient { get; set; }

    /// <summary>The Entra object ID (UserId) of the athlete created for this scenario.</summary>
    public string? AthleteUserId { get; set; }

    /// <summary>The OAuth state token pre-registered in the in-memory state service for this scenario.</summary>
    public string? StateToken { get; set; }

    /// <summary>The raw HTTP response from the most recent API call.</summary>
    public HttpResponseMessage? LastResponse { get; set; }
}
