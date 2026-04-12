namespace MarathonTraining.Api.IntegrationTests.Support;

public sealed class TrainingLoadContext
{
    public ApiWebApplicationFactory? Factory { get; set; }
    public HttpClient? HttpClient { get; set; }
    public string? AthleteUserId { get; set; }
    public Guid? AthleteProfileId { get; set; }
    public HttpResponseMessage? LastResponse { get; set; }
}
