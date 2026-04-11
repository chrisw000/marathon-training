using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MarathonTraining.Application.Strava;
using Microsoft.Extensions.Configuration;

namespace MarathonTraining.Infrastructure.Strava;

public sealed class StravaTokenService(HttpClient httpClient, IConfiguration configuration) : IStravaTokenService
{
    private readonly string _clientId =
        configuration["Strava:ClientId"] ?? throw new InvalidOperationException("Strava:ClientId is not configured.");

    private readonly string _clientSecret =
        configuration["Strava:ClientSecret"] ?? throw new InvalidOperationException("Strava:ClientSecret is not configured.");

    public async Task<StravaTokenResponse> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync(
            "oauth/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret,
                ["code"] = code,
                ["grant_type"] = "authorization_code",
            }),
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<StravaAuthCodeResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Empty response from Strava token endpoint.");

        return new StravaTokenResponse(
            body.AccessToken,
            body.RefreshToken,
            DateTimeOffset.FromUnixTimeSeconds(body.ExpiresAt),
            body.Athlete.Id);
    }

    public async Task<StravaTokenResponse> RefreshTokenAsync(
        string refreshToken,
        long existingStravaAthleteId,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync(
            "oauth/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret,
                ["refresh_token"] = refreshToken,
                ["grant_type"] = "refresh_token",
            }),
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<StravaRefreshResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Empty response from Strava token refresh endpoint.");

        // Strava does not return athlete data on refresh — carry the existing ID through
        return new StravaTokenResponse(
            body.AccessToken,
            body.RefreshToken,
            DateTimeOffset.FromUnixTimeSeconds(body.ExpiresAt),
            existingStravaAthleteId);
    }

    // ── Private response DTOs ──────────────────────────────────────────────

    private sealed record StravaAuthCodeResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("refresh_token")] string RefreshToken,
        [property: JsonPropertyName("expires_at")] long ExpiresAt,
        [property: JsonPropertyName("athlete")] StravaAthleteSummary Athlete);

    private sealed record StravaAthleteSummary(
        [property: JsonPropertyName("id")] long Id);

    private sealed record StravaRefreshResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("refresh_token")] string RefreshToken,
        [property: JsonPropertyName("expires_at")] long ExpiresAt);
}
