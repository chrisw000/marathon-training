using Bogus;
using MarathonTraining.Domain.Aggregates;
using MarathonTraining.Domain.Enums;
using MarathonTraining.Domain.ValueObjects;

namespace MarathonTraining.Application.Tests.Fakers;

internal static class AthleteProfileFaker
{
    private static readonly Faker Fake = new();

    internal static Domain.Aggregates.AthleteProfile Default()
    {
        var profile = new Domain.Aggregates.AthleteProfile(
            id: Guid.NewGuid(),
            userId: Fake.Random.Guid().ToString(),
            displayName: Fake.Name.FullName(),
            createdAt: DateTimeOffset.UtcNow.AddDays(-Fake.Random.Int(1, 365)));

        var zones = HeartRateZones.Create(
            restingHr: Fake.Random.Int(40, 65),
            maxHr: Fake.Random.Int(170, 200),
            thresholdHr: Fake.Random.Int(140, 169));

        var ftp = FunctionalThresholdPower.Create(Fake.Random.Int(100, 400));

        profile.UpdatePhysiology(zones, ftp);
        profile.UpdateTrainingPhase(Fake.PickRandom<TrainingPhase>());

        return profile;
    }

    internal static Domain.Aggregates.AthleteProfile WithoutPhysiology()
        => new(
            id: Guid.NewGuid(),
            userId: Fake.Random.Guid().ToString(),
            displayName: Fake.Name.FullName(),
            createdAt: DateTimeOffset.UtcNow.AddDays(-Fake.Random.Int(1, 365)));
}
