namespace MarathonTraining.Api.IntegrationTests.Support;

/// <summary>
/// Holds the factory and client for endpoint authentication scenarios.
/// No WireMock or DB seeding needed — 401 is returned before any handler runs.
/// </summary>
public sealed class EndpointAuthContext
{
    public ApiWebApplicationFactory? Factory { get; set; }
    public HttpClient? HttpClient { get; set; }
    public HttpResponseMessage? LastResponse { get; set; }
}
