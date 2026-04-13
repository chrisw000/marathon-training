using MarathonTraining.Domain.Enums;
using MarathonTraining.Domain.ValueObjects;

namespace MarathonTraining.Domain.Aggregates;

public class Activity
{
    public Guid Id { get; private set; }
    public Guid TrainingWeekId { get; private set; }
    public Guid AthleteProfileId { get; private set; }
    public ActivityType ActivityType { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTimeOffset StartedAt { get; private set; }
    public int DurationSeconds { get; private set; }
    public double? DistanceMetres { get; private set; }
    public TssScore? TssScore { get; private set; }

    // Raw physiological inputs — populated from Strava or manual entry
    public int? AverageHeartRateBpm { get; private set; }
    public int? MaxHeartRateBpm { get; private set; }
    public int? NormalisedPowerWatts { get; private set; }
    public int? RpeValue { get; private set; }

    // Strava-specific fields
    public long? StravaActivityId { get; private set; }
    public string? StravaActivityType { get; private set; }
    public bool HasHeartRate { get; private set; }
    public int? AveragePowerWatts { get; private set; }
    public bool IsDevicePower { get; private set; }
    public double? AverageSpeedMetresPerSecond { get; private set; }

    // Data origin
    public string? ExternalSource { get; private set; }

    protected Activity() { }

    public Activity(
        Guid id,
        Guid trainingWeekId,
        Guid athleteProfileId,
        ActivityType activityType,
        string name,
        DateTimeOffset startedAt,
        int durationSeconds,
        double? distanceMetres,
        TssScore? tssScore,
        int? averageHeartRateBpm = null,
        int? maxHeartRateBpm = null,
        int? normalisedPowerWatts = null,
        int? rpeValue = null)
    {
        Id = id;
        TrainingWeekId = trainingWeekId;
        AthleteProfileId = athleteProfileId;
        ActivityType = activityType;
        Name = name;
        StartedAt = startedAt;
        DurationSeconds = durationSeconds;
        DistanceMetres = distanceMetres;
        TssScore = tssScore;
        AverageHeartRateBpm = averageHeartRateBpm;
        MaxHeartRateBpm = maxHeartRateBpm;
        NormalisedPowerWatts = normalisedPowerWatts;
        RpeValue = rpeValue;
    }

    /// <summary>
    /// Creates an Activity from a Strava sync, mapping Strava's sport_type string to the
    /// domain ActivityType enum. Infrastructure calls this with individual fields so the
    /// domain layer has no dependency on StravaActivityDto.
    /// </summary>
    public static Activity CreateFromStrava(
        Guid trainingWeekId,
        Guid athleteProfileId,
        long stravaId,
        string name,
        string stravaActivityType,
        DateTimeOffset startedAt,
        int durationSeconds,
        double? distanceMetres,
        int? averageHeartRate,
        int? maxHeartRate,
        bool hasHeartRate,
        int? averagePowerWatts,
        bool isDevicePower,
        double? averageSpeedMetresPerSecond)
    {
        var activityType = MapStravaActivityType(stravaActivityType);

        return new Activity
        {
            Id = Guid.NewGuid(),
            TrainingWeekId = trainingWeekId,
            AthleteProfileId = athleteProfileId,
            ActivityType = activityType,
            Name = name,
            StartedAt = startedAt,
            DurationSeconds = durationSeconds,
            DistanceMetres = distanceMetres,
            AverageHeartRateBpm = averageHeartRate,
            MaxHeartRateBpm = maxHeartRate,
            HasHeartRate = hasHeartRate,
            AveragePowerWatts = averagePowerWatts,
            NormalisedPowerWatts = averagePowerWatts,   // NP ≈ weighted avg watts from Strava
            IsDevicePower = isDevicePower,
            AverageSpeedMetresPerSecond = averageSpeedMetresPerSecond,
            StravaActivityId = stravaId,
            StravaActivityType = stravaActivityType,
            ExternalSource = "Strava",
        };
    }

    public void AssignTss(TssScore tss)
    {
        TssScore = tss;
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private static ActivityType MapStravaActivityType(string stravaType) => stravaType switch
    {
        "Run" or "VirtualRun" or "TrailRun" => ActivityType.Run,
        "Ride" or "VirtualRide" or "EBikeRide" or "MountainBikeRide" => ActivityType.Ride,
        "WeightTraining" or "Workout" or "Crossfit" or "Yoga" => ActivityType.Strength,
        _ => ActivityType.Run,   // safe default for unknown types
    };
}
