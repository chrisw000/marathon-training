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

    public void AssignTss(TssScore tss)
    {
        TssScore = tss;
    }
}
