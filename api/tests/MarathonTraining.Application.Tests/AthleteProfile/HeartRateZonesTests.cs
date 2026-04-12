using MarathonTraining.Domain.Exceptions;
using MarathonTraining.Domain.ValueObjects;

namespace MarathonTraining.Application.Tests.Athlete;

public sealed class HeartRateZonesTests
{
    [Fact]
    public void Create_ValidZones_Succeeds()
    {
        var zones = HeartRateZones.Create(restingHr: 50, maxHr: 185, thresholdHr: 165);

        zones.RestingHr.Should().Be(50);
        zones.MaxHr.Should().Be(185);
        zones.ThresholdHr.Should().Be(165);
    }

    [Fact]
    public void Create_RestingHrOfZero_ThrowsDomainException()
    {
        var act = () => HeartRateZones.Create(restingHr: 0, maxHr: 185, thresholdHr: 165);

        act.Should().Throw<DomainException>()
            .WithMessage("*resting*");
    }

    [Fact]
    public void Create_RestingHrGreaterThanMaxHr_ThrowsDomainException()
    {
        var act = () => HeartRateZones.Create(restingHr: 190, maxHr: 185, thresholdHr: 165);

        act.Should().Throw<DomainException>()
            .WithMessage("*resting*");
    }

    [Fact]
    public void Create_RestingHrEqualToMaxHr_ThrowsDomainException()
    {
        var act = () => HeartRateZones.Create(restingHr: 185, maxHr: 185, thresholdHr: 165);

        act.Should().Throw<DomainException>()
            .WithMessage("*resting*");
    }

    [Fact]
    public void Create_MaxHrAtOrBelowOneHundred_ThrowsDomainException()
    {
        var act = () => HeartRateZones.Create(restingHr: 50, maxHr: 100, thresholdHr: 75);

        act.Should().Throw<DomainException>()
            .WithMessage("*Maximum*");
    }

    [Fact]
    public void Create_MaxHrAtOrAboveTwoThirty_ThrowsDomainException()
    {
        var act = () => HeartRateZones.Create(restingHr: 50, maxHr: 230, thresholdHr: 165);

        act.Should().Throw<DomainException>()
            .WithMessage("*Maximum*");
    }

    [Fact]
    public void Create_ThresholdHrAtOrBelowRestingHr_ThrowsDomainException()
    {
        var act = () => HeartRateZones.Create(restingHr: 60, maxHr: 185, thresholdHr: 60);

        act.Should().Throw<DomainException>()
            .WithMessage("*Threshold*");
    }

    [Fact]
    public void Create_ThresholdHrAtOrAboveMaxHr_ThrowsDomainException()
    {
        var act = () => HeartRateZones.Create(restingHr: 50, maxHr: 185, thresholdHr: 185);

        act.Should().Throw<DomainException>()
            .WithMessage("*Threshold*");
    }

    [Fact]
    public void Create_ExceptionMessages_AreDescriptive()
    {
        var actResting = () => HeartRateZones.Create(0, 185, 165);
        var actMax = () => HeartRateZones.Create(50, 100, 80);
        var actThreshold = () => HeartRateZones.Create(50, 185, 185);

        actResting.Should().Throw<DomainException>()
            .WithMessage("*resting*");
        actMax.Should().Throw<DomainException>()
            .WithMessage("*Maximum*");
        actThreshold.Should().Throw<DomainException>()
            .WithMessage("*Threshold*");
    }
}
