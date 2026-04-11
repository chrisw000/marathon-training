namespace MarathonTraining.Api.IntegrationTests.Support;

public sealed class AthleteProfileContext
{
    public ApiWebApplicationFactory? Factory { get; set; }
    public HttpClient? HttpClient { get; set; }
    public string? AthleteUserId { get; set; }
    public HttpResponseMessage? LastResponse { get; set; }
}
