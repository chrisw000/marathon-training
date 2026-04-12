using Bogus;
using MarathonTraining.Domain.Aggregates;
using MarathonTraining.Domain.Enums;
using MarathonTraining.Domain.ValueObjects;

namespace MarathonTraining.Application.Tests.Fakers;

internal static class ActivityFaker
{
    private static readonly Faker Fake = new();

    internal static Activity Run(
        Guid? athleteProfileId = null,
        Guid? trainingWeekId = null,
        int? avgHr = null,
        int? durationSeconds = null,
        TssScore? tssScore = null) =>
        new(
            id: Guid.NewGuid(),
            trainingWeekId: trainingWeekId ?? Guid.NewGuid(),
            athleteProfileId: athleteProfileId ?? Guid.NewGuid(),
            activityType: ActivityType.Run,
            name: Fake.Lorem.Word() + " run",
            startedAt: DateTimeOffset.UtcNow.AddDays(-Fake.Random.Int(1, 90)),
            durationSeconds: durationSeconds ?? Fake.Random.Int(1800, 7200),
            distanceMetres: Fake.Random.Double(3000, 25000),
            tssScore: tssScore,
            averageHeartRateBpm: avgHr ?? Fake.Random.Int(120, 165),
            maxHeartRateBpm: 185);

    internal static Activity Ride(
        Guid? athleteProfileId = null,
        Guid? trainingWeekId = null,
        int? npWatts = null,
        int? durationSeconds = null,
        TssScore? tssScore = null) =>
        new(
            id: Guid.NewGuid(),
            trainingWeekId: trainingWeekId ?? Guid.NewGuid(),
            athleteProfileId: athleteProfileId ?? Guid.NewGuid(),
            activityType: ActivityType.Ride,
            name: Fake.Lorem.Word() + " ride",
            startedAt: DateTimeOffset.UtcNow.AddDays(-Fake.Random.Int(1, 90)),
            durationSeconds: durationSeconds ?? Fake.Random.Int(1800, 7200),
            distanceMetres: Fake.Random.Double(10000, 80000),
            tssScore: tssScore,
            normalisedPowerWatts: npWatts ?? Fake.Random.Int(150, 280));

    internal static Activity Strength(
        Guid? athleteProfileId = null,
        Guid? trainingWeekId = null,
        int? rpe = null,
        int? durationSeconds = null,
        TssScore? tssScore = null) =>
        new(
            id: Guid.NewGuid(),
            trainingWeekId: trainingWeekId ?? Guid.NewGuid(),
            athleteProfileId: athleteProfileId ?? Guid.NewGuid(),
            activityType: ActivityType.Strength,
            name: Fake.Lorem.Word() + " strength",
            startedAt: DateTimeOffset.UtcNow.AddDays(-Fake.Random.Int(1, 90)),
            durationSeconds: durationSeconds ?? Fake.Random.Int(1800, 5400),
            distanceMetres: null,
            tssScore: tssScore,
            rpeValue: rpe ?? Fake.Random.Int(5, 8));

    internal static Activity WithTss(Activity activity, decimal tssValue)
    {
        activity.AssignTss(TssScore.Create(tssValue));
        return activity;
    }
}
