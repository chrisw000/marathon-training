namespace MarathonTraining.Api.IntegrationTests.Support;

/// <summary>
/// Holds all state that flows between the Hooks and Steps within a single
/// Strava connection management scenario (status, disconnect, authorise).
/// Reqnroll creates one instance per scenario and injects it via constructor.
/// </summary>
public sealed class StravaConnectionContext
{
    /// <summary>The in-process test host, configured with the test database.</summary>
    public ApiWebApplicationFactory? Factory { get; set; }

    /// <summary>The HTTP client for driving the API.</summary>
    public HttpClient? HttpClient { get; set; }

    /// <summary>The Entra object ID (UserId) of the athlete created for this scenario.</summary>
    public string? AthleteUserId { get; set; }

    /// <summary>The raw HTTP response from the most recent API call.</summary>
    public HttpResponseMessage? LastResponse { get; set; }
}
