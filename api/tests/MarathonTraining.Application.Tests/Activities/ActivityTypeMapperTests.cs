using MarathonTraining.Domain.Aggregates;
using MarathonTraining.Domain.Enums;

namespace MarathonTraining.Application.Tests.Activities;

/// <summary>
/// Tests for the sport_type → ActivityType mapping on Activity.CreateFromStrava.
/// The mapping is a private static method; exercising it via the public factory
/// validates the observable domain result without exposing internals.
/// </summary>
public sealed class ActivityTypeMapperTests
{
    private static Activity MakeActivity(string sportType) =>
        Activity.CreateFromStrava(
            trainingWeekId: Guid.NewGuid(),
            athleteProfileId: Guid.NewGuid(),
            stravaId: 1L,
            name: "Test",
            stravaActivityType: sportType,
            startedAt: DateTimeOffset.UtcNow,
            durationSeconds: 3600,
            distanceMetres: null,
            averageHeartRate: null,
            maxHeartRate: null,
            hasHeartRate: false,
            averagePowerWatts: null,
            isDevicePower: false,
            averageSpeedMetresPerSecond: null);

    [Theory]
    [InlineData("Run", ActivityType.Run)]
    [InlineData("VirtualRun", ActivityType.Run)]
    [InlineData("TrailRun", ActivityType.Run)]
    [InlineData("Ride", ActivityType.Ride)]
    [InlineData("VirtualRide", ActivityType.Ride)]
    [InlineData("EBikeRide", ActivityType.Ride)]
    [InlineData("MountainBikeRide", ActivityType.Ride)]
    [InlineData("WeightTraining", ActivityType.Strength)]
    [InlineData("Workout", ActivityType.Strength)]
    [InlineData("Crossfit", ActivityType.Strength)]
    [InlineData("Yoga", ActivityType.Strength)]
    [InlineData("Swim", ActivityType.Run)]     // unknown type falls back to Run
    [InlineData("Soccer", ActivityType.Run)]   // unknown type falls back to Run
    public void CreateFromStrava_MapsStravaTypeToActivityType(
        string sportType,
        ActivityType expectedType)
    {
        var activity = MakeActivity(sportType);

        activity.ActivityType.Should().Be(expectedType);
        activity.StravaActivityType.Should().Be(sportType);
    }

    [Fact]
    public void CreateFromStrava_SetsStravaFields()
    {
        const long stravaId = 123456789L;
        var activity = Activity.CreateFromStrava(
            trainingWeekId: Guid.NewGuid(),
            athleteProfileId: Guid.NewGuid(),
            stravaId: stravaId,
            name: "Morning Run",
            stravaActivityType: "Run",
            startedAt: DateTimeOffset.UtcNow,
            durationSeconds: 3600,
            distanceMetres: 10000,
            averageHeartRate: 145,
            maxHeartRate: 175,
            hasHeartRate: true,
            averagePowerWatts: null,
            isDevicePower: false,
            averageSpeedMetresPerSecond: 2.78);

        activity.StravaActivityId.Should().Be(stravaId);
        activity.ExternalSource.Should().Be("Strava");
        activity.HasHeartRate.Should().BeTrue();
        activity.AverageHeartRateBpm.Should().Be(145);
        activity.MaxHeartRateBpm.Should().Be(175);
        activity.DistanceMetres.Should().BeApproximately(10000, 0.01);
        activity.AverageSpeedMetresPerSecond.Should().BeApproximately(2.78, 0.001);
    }

    [Fact]
    public void CreateFromStrava_SetsPowerFields()
    {
        var activity = Activity.CreateFromStrava(
            trainingWeekId: Guid.NewGuid(),
            athleteProfileId: Guid.NewGuid(),
            stravaId: 42L,
            name: "Zwift ride",
            stravaActivityType: "VirtualRide",
            startedAt: DateTimeOffset.UtcNow,
            durationSeconds: 3600,
            distanceMetres: 40000,
            averageHeartRate: null,
            maxHeartRate: null,
            hasHeartRate: false,
            averagePowerWatts: 220,
            isDevicePower: true,
            averageSpeedMetresPerSecond: 11.0);

        activity.AveragePowerWatts.Should().Be(220);
        activity.NormalisedPowerWatts.Should().Be(220); // NP ≈ weighted avg watts from Strava
        activity.IsDevicePower.Should().BeTrue();
    }
}
