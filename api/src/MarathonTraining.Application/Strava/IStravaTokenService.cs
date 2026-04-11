namespace MarathonTraining.Application.Strava;

public record StravaTokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    long StravaAthleteId);

public interface IStravaTokenService
{
    Task<StravaTokenResponse> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<StravaTokenResponse> RefreshTokenAsync(string refreshToken, long existingStravaAthleteId, CancellationToken cancellationToken = default);
}
