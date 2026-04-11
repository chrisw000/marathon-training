namespace MarathonTraining.Domain.Aggregates;

public class StravaConnection
{
    public Guid AthleteProfileId { get; private set; }
    public long StravaAthleteId { get; private set; }
    public string AccessToken { get; private set; } = string.Empty;
    public string RefreshToken { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }

    protected StravaConnection() { }

    public StravaConnection(
        Guid athleteProfileId,
        long stravaAthleteId,
        string accessToken,
        string refreshToken,
        DateTimeOffset expiresAt)
    {
        AthleteProfileId = athleteProfileId;
        StravaAthleteId = stravaAthleteId;
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ExpiresAt = expiresAt;
    }

    public void Update(string accessToken, string refreshToken, DateTimeOffset expiresAt)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ExpiresAt = expiresAt;
    }
}
