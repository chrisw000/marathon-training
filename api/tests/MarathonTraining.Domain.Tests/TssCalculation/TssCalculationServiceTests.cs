using MarathonTraining.Domain.Enums;
using MarathonTraining.Domain.Exceptions;
using MarathonTraining.Domain.Services;
using MarathonTraining.Domain.ValueObjects;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace MarathonTraining.Domain.Tests.TssCalculation;

public sealed class TssCalculationServiceTests
{
    private static TssCalculationInputs MakeInputs(ActivityType type) =>
        new(
            ActivityType: type,
            Duration: ActivityDuration.Create(3600),
            HeartRate: null,
            AthleteHrZones: null,
            NormalisedPower: null,
            Ftp: null,
            Rpe: null);

    [Fact]
    public void Calculate_RunActivity_RoutesToRunCalculator()
    {
        var runCalc = Substitute.For<ITssCalculator>();
        var rideCalc = Substitute.For<ITssCalculator>();
        var strengthCalc = Substitute.For<ITssCalculator>();

        var expected = TssScore.Create(75m);
        var inputs = MakeInputs(ActivityType.Run);

        // Run calculator accepts; others reject with type-mismatch message
        runCalc.Calculate(inputs).Returns(expected);
        rideCalc.Calculate(inputs).Throws(new DomainException("cannot handle activity type 'Run'."));
        strengthCalc.Calculate(inputs).Throws(new DomainException("cannot handle activity type 'Run'."));

        var sut = new TssCalculationService([rideCalc, strengthCalc, runCalc]);
        var result = sut.Calculate(inputs);

        result.Should().Be(expected);
    }

    [Fact]
    public void Calculate_RideActivity_RoutesToRideCalculator()
    {
        var runCalc = Substitute.For<ITssCalculator>();
        var rideCalc = Substitute.For<ITssCalculator>();

        var expected = TssScore.Create(90m);
        var inputs = MakeInputs(ActivityType.Ride);

        runCalc.Calculate(inputs).Throws(new DomainException("cannot handle activity type 'Ride'."));
        rideCalc.Calculate(inputs).Returns(expected);

        var sut = new TssCalculationService([runCalc, rideCalc]);
        var result = sut.Calculate(inputs);

        result.Should().Be(expected);
    }

    [Fact]
    public void Calculate_StrengthActivity_RoutesToStrengthCalculator()
    {
        var runCalc = Substitute.For<ITssCalculator>();
        var strengthCalc = Substitute.For<ITssCalculator>();

        var expected = TssScore.Create(25m);
        var inputs = MakeInputs(ActivityType.Strength);

        runCalc.Calculate(inputs).Throws(new DomainException("cannot handle activity type 'Strength'."));
        strengthCalc.Calculate(inputs).Returns(expected);

        var sut = new TssCalculationService([runCalc, strengthCalc]);
        var result = sut.Calculate(inputs);

        result.Should().Be(expected);
    }

    [Fact]
    public void Calculate_NoMatchingCalculator_ThrowsDomainExceptionWithDescriptiveMessage()
    {
        var calc = Substitute.For<ITssCalculator>();
        calc.Calculate(Arg.Any<TssCalculationInputs>())
            .Throws(new DomainException("cannot handle activity type 'Run'."));

        var sut = new TssCalculationService([calc]);
        var inputs = MakeInputs(ActivityType.Run);

        var act = () => sut.Calculate(inputs);

        act.Should().Throw<DomainException>().WithMessage("*No TSS calculator*");
    }

    [Fact]
    public void Calculate_CalculatorThrowsDomainExceptionForDataReason_ReThrowsWithContext()
    {
        var calc = Substitute.For<ITssCalculator>();
        calc.Calculate(Arg.Any<TssCalculationInputs>())
            .Throws(new DomainException("RPE score is required."));

        var sut = new TssCalculationService([calc]);
        var inputs = MakeInputs(ActivityType.Strength);

        var act = () => sut.Calculate(inputs);

        act.Should().Throw<DomainException>().WithMessage("*TSS calculation failed*Strength*");
    }
}
