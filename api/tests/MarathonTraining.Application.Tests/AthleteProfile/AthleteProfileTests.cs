using Bogus;
using MarathonTraining.Application.Tests.Fakers;
using MarathonTraining.Domain.Enums;
using MarathonTraining.Domain.Events;
using MarathonTraining.Domain.ValueObjects;

namespace MarathonTraining.Application.Tests.Athlete;

public sealed class AthleteProfileTests
{
    private static readonly Faker Fake = new();

    [Fact]
    public void UpdatePhysiology_SetsHeartRateZonesAndFtp()
    {
        var profile = AthleteProfileFaker.WithoutPhysiology();
        var zones = HeartRateZones.Create(55, 185, 168);
        var ftp = FunctionalThresholdPower.Create(260);

        profile.UpdatePhysiology(zones, ftp);

        profile.HeartRateZones.Should().NotBeNull();
        profile.HeartRateZones!.RestingHr.Should().Be(55);
        profile.HeartRateZones.MaxHr.Should().Be(185);
        profile.HeartRateZones.ThresholdHr.Should().Be(168);
        profile.Ftp.Should().NotBeNull();
        profile.Ftp!.Watts.Should().Be(260);
    }

    [Fact]
    public void UpdatePhysiology_RaisesAthletePhysiologyUpdatedEvent()
    {
        var profile = AthleteProfileFaker.WithoutPhysiology();
        var zones = HeartRateZones.Create(55, 185, 168);
        var ftp = FunctionalThresholdPower.Create(260);

        profile.UpdatePhysiology(zones, ftp);

        var domainEvent = profile.DomainEvents
            .OfType<AthletePhysiologyUpdatedEvent>()
            .Single();

        domainEvent.AthleteId.Should().Be(profile.Id);
        domainEvent.Watts.Should().Be(260);
        domainEvent.RestingHr.Should().Be(55);
        domainEvent.MaxHr.Should().Be(185);
        domainEvent.ThresholdHr.Should().Be(168);
    }

    [Fact]
    public void UpdateTrainingPhase_ChangesPhaseCorrectly()
    {
        var profile = AthleteProfileFaker.WithoutPhysiology();

        profile.UpdateTrainingPhase(TrainingPhase.Build);

        profile.CurrentPhase.Should().Be(TrainingPhase.Build);
    }

    [Fact]
    public void UpdateTrainingPhase_RaisesTrainingPhaseChangedEventWithCorrectOldPhase()
    {
        var profile = AthleteProfileFaker.WithoutPhysiology();
        var originalPhase = profile.CurrentPhase;

        profile.UpdateTrainingPhase(TrainingPhase.Peak);

        var domainEvent = profile.DomainEvents
            .OfType<TrainingPhaseChangedEvent>()
            .Single();

        domainEvent.AthleteId.Should().Be(profile.Id);
        domainEvent.OldPhase.Should().Be(originalPhase);
        domainEvent.NewPhase.Should().Be(TrainingPhase.Peak);
    }

    [Fact]
    public void RecordSync_SetsLastSyncedAtToApproximatelyUtcNow()
    {
        var profile = AthleteProfileFaker.Default();
        var before = DateTimeOffset.UtcNow;

        profile.RecordSync();

        var after = DateTimeOffset.UtcNow;

        profile.LastSyncedAt.Should().NotBeNull();
        profile.LastSyncedAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }
}
