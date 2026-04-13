using Bogus;
using MarathonTraining.Application.Activities;

namespace MarathonTraining.Application.Tests.Fakers;

internal static class StravaActivitySummaryFaker
{
    private static readonly Faker Fake = new();

    internal static StravaActivitySummary Run(long? stravaId = null) =>
        new(
            StravaId: stravaId ?? Fake.Random.Long(1_000_000, 999_999_999),
            Name: Fake.Lorem.Word() + " Run",
            SportType: "Run",
            StartedAt: DateTimeOffset.UtcNow.AddDays(-Fake.Random.Int(1, 90)),
            MovingTimeSeconds: Fake.Random.Int(1800, 7200),
            DistanceMetres: Fake.Random.Double(3000, 25000),
            AverageHeartRate: Fake.Random.Int(130, 160),
            MaxHeartRate: Fake.Random.Int(165, 185),
            HasHeartRate: true,
            AveragePowerWatts: null,
            IsDevicePower: false,
            AverageSpeedMetresPerSecond: Fake.Random.Double(2.5, 5.0));

    internal static StravaActivitySummary Ride(long? stravaId = null) =>
        new(
            StravaId: stravaId ?? Fake.Random.Long(1_000_000, 999_999_999),
            Name: Fake.Lorem.Word() + " Ride",
            SportType: "Ride",
            StartedAt: DateTimeOffset.UtcNow.AddDays(-Fake.Random.Int(1, 90)),
            MovingTimeSeconds: Fake.Random.Int(3600, 14400),
            DistanceMetres: Fake.Random.Double(20000, 100000),
            AverageHeartRate: Fake.Random.Int(130, 165),
            MaxHeartRate: Fake.Random.Int(170, 185),
            HasHeartRate: true,
            AveragePowerWatts: Fake.Random.Int(150, 280),
            IsDevicePower: true,
            AverageSpeedMetresPerSecond: Fake.Random.Double(7.0, 12.0));

    internal static StravaActivitySummary WeightTraining(long? stravaId = null) =>
        new(
            StravaId: stravaId ?? Fake.Random.Long(1_000_000, 999_999_999),
            Name: "Weights",
            SportType: "WeightTraining",
            StartedAt: DateTimeOffset.UtcNow.AddDays(-Fake.Random.Int(1, 90)),
            MovingTimeSeconds: Fake.Random.Int(1800, 5400),
            DistanceMetres: null,
            AverageHeartRate: null,
            MaxHeartRate: null,
            HasHeartRate: false,
            AveragePowerWatts: null,
            IsDevicePower: false,
            AverageSpeedMetresPerSecond: null);
}
