using System.Net;
using System.Net.Http.Json;
using MarathonTraining.Application.Activities;
using MarathonTraining.Application.Common.Interfaces;
using MarathonTraining.Infrastructure.Strava.Exceptions;

namespace MarathonTraining.Infrastructure.Strava;

/// <summary>
/// Typed HTTP client for Strava's athlete activities endpoint.
/// Implements IStravaActivityClient (Application interface) and maps
/// StravaActivityDto → StravaActivitySummary before returning.
/// </summary>
public sealed class StravaActivityClient(HttpClient httpClient) : IStravaActivityClient
{
    public async Task<IReadOnlyList<StravaActivitySummary>> GetActivitiesAsync(
        string accessToken,
        long? afterEpoch,
        int page,
        int perPage = 100,
        CancellationToken cancellationToken = default)
    {
        // Build query — after and page cannot be combined (Strava API constraint).
        var query = afterEpoch.HasValue
            ? $"after={afterEpoch.Value}&per_page={perPage}"
            : $"page={page}&per_page={perPage}";

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/v3/athlete/activities?{query}");

        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new StravaUnauthorisedException();

        if (response.StatusCode == (HttpStatusCode)429)
            throw new StravaRateLimitException();

        if (!response.IsSuccessStatusCode)
            throw new StravaApiException(
                (int)response.StatusCode,
                $"Strava API returned {(int)response.StatusCode} {response.ReasonPhrase}.");

        var dtos = await response.Content
            .ReadFromJsonAsync<StravaActivityDto[]>(cancellationToken: cancellationToken)
            ?? [];

        return dtos.Select(Map).ToList().AsReadOnly();
    }

    private static StravaActivitySummary Map(StravaActivityDto dto) =>
        new(
            StravaId: dto.Id,
            Name: dto.Name,
            SportType: string.IsNullOrEmpty(dto.SportType) ? (dto.Type ?? "Run") : dto.SportType,
            StartedAt: dto.StartDate,
            MovingTimeSeconds: dto.MovingTime,
            DistanceMetres: dto.Distance is null ? null : (double?)dto.Distance,
            AverageHeartRate: dto.AverageHeartrate is null ? null : (int?)dto.AverageHeartrate,
            MaxHeartRate: dto.MaxHeartrate is null ? null : (int?)dto.MaxHeartrate,
            HasHeartRate: dto.HasHeartrate,
            AveragePowerWatts: dto.WeightedAverageWatts is null ? null : (int?)dto.WeightedAverageWatts,
            IsDevicePower: dto.DeviceWatts ?? false,
            AverageSpeedMetresPerSecond: dto.AverageSpeed is null ? null : (double?)dto.AverageSpeed);
}
