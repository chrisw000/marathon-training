using MarathonTraining.Domain.Enums;
using MarathonTraining.Domain.ValueObjects;

namespace MarathonTraining.Domain.Aggregates;

public class Activity
{
    public Guid Id { get; private set; }
    public Guid TrainingWeekId { get; private set; }
    public ActivityType ActivityType { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTimeOffset StartedAt { get; private set; }
    public int DurationSeconds { get; private set; }
    public double? DistanceMetres { get; private set; }
    public TssScore? TssScore { get; private set; }

    protected Activity() { }

    public Activity(
        Guid id,
        Guid trainingWeekId,
        ActivityType activityType,
        string name,
        DateTimeOffset startedAt,
        int durationSeconds,
        double? distanceMetres,
        TssScore? tssScore)
    {
        Id = id;
        TrainingWeekId = trainingWeekId;
        ActivityType = activityType;
        Name = name;
        StartedAt = startedAt;
        DurationSeconds = durationSeconds;
        DistanceMetres = distanceMetres;
        TssScore = tssScore;
    }
}
