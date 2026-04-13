namespace MarathonTraining.Application.Activities;

/// <summary>
/// Application-layer representation of a Strava activity summary.
/// Infrastructure maps from StravaActivityDto to this record so the Application
/// layer has no dependency on Infrastructure types.
/// </summary>
public sealed record StravaActivitySummary(
    long StravaId,
    string Name,
    string SportType,
    DateTimeOffset StartedAt,
    int MovingTimeSeconds,
    double? DistanceMetres,
    int? AverageHeartRate,
    int? MaxHeartRate,
    bool HasHeartRate,
    int? AveragePowerWatts,
    bool IsDevicePower,
    double? AverageSpeedMetresPerSecond);
