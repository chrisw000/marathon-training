using System.Text.Json.Serialization;

namespace MarathonTraining.Infrastructure.Strava;

/// <summary>
/// Maps the fields we use from Strava's SummaryActivity JSON response.
/// Only fields consumed by the sync handler are included — unmapped fields are ignored.
/// </summary>
public sealed record StravaActivityDto(
    [property: JsonPropertyName("id")]                      long Id,
    [property: JsonPropertyName("name")]                    string Name,
    [property: JsonPropertyName("sport_type")]              string SportType,
    [property: JsonPropertyName("type")]                    string? Type,
    [property: JsonPropertyName("start_date")]              DateTimeOffset StartDate,
    [property: JsonPropertyName("elapsed_time")]            int ElapsedTime,
    [property: JsonPropertyName("moving_time")]             int MovingTime,
    [property: JsonPropertyName("distance")]                float? Distance,
    [property: JsonPropertyName("average_speed")]           float? AverageSpeed,
    [property: JsonPropertyName("average_heartrate")]       float? AverageHeartrate,
    [property: JsonPropertyName("max_heartrate")]           float? MaxHeartrate,
    [property: JsonPropertyName("has_heartrate")]           bool HasHeartrate,
    [property: JsonPropertyName("weighted_average_watts")]  float? WeightedAverageWatts,
    [property: JsonPropertyName("device_watts")]            bool? DeviceWatts);
