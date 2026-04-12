using MarathonTraining.Domain.Enums;
using MarathonTraining.Domain.Exceptions;
using MarathonTraining.Domain.Services;
using MarathonTraining.Domain.ValueObjects;

namespace MarathonTraining.Domain.Tests.TssCalculation;

public sealed class StrengthTssCalculatorTests
{
    private readonly StrengthTssCalculator _sut = new();

    private static TssCalculationInputs Inputs(int durationSeconds, int? rpe) =>
        new(
            ActivityType: ActivityType.Strength,
            Duration: ActivityDuration.Create(durationSeconds),
            HeartRate: null,
            AthleteHrZones: null,
            NormalisedPower: null,
            Ftp: null,
            Rpe: rpe.HasValue ? RpeScore.Create(rpe.Value) : null);

    [Fact]
    public void Calculate_45MinRpe6_TssApprox24_75()
    {
        var result = _sut.Calculate(Inputs(2700, 6));

        ((double)result.Value).Should().BeApproximately(24.75, 0.1);
    }

    [Fact]
    public void Calculate_60MinRpe8_TssApprox44()
    {
        var result = _sut.Calculate(Inputs(3600, 8));

        ((double)result.Value).Should().BeApproximately(44.0, 0.1);
    }

    [Fact]
    public void Calculate_30MinRpe4_TssApprox11()
    {
        var result = _sut.Calculate(Inputs(1800, 4));

        ((double)result.Value).Should().BeApproximately(11.0, 0.1);
    }

    [Fact]
    public void Calculate_BoundaryRpe1_60Min_TssApprox5_5()
    {
        var result = _sut.Calculate(Inputs(3600, 1));

        ((double)result.Value).Should().BeApproximately(5.5, 0.1);
    }

    [Fact]
    public void Calculate_BoundaryRpe10_60Min_TssApprox55()
    {
        var result = _sut.Calculate(Inputs(3600, 10));

        ((double)result.Value).Should().BeApproximately(55.0, 0.1);
    }

    [Fact]
    public void Calculate_NullRpe_ThrowsDomainException()
    {
        var act = () => _sut.Calculate(Inputs(3600, null));

        act.Should().Throw<DomainException>().WithMessage("*RPE*");
    }

    [Fact]
    public void Calculate_WrongActivityType_ThrowsDomainException()
    {
        var inputs = new TssCalculationInputs(
            ActivityType: ActivityType.Run,
            Duration: ActivityDuration.Create(3600),
            HeartRate: null,
            AthleteHrZones: null,
            NormalisedPower: null,
            Ftp: null,
            Rpe: RpeScore.Create(6));

        var act = () => _sut.Calculate(inputs);

        act.Should().Throw<DomainException>().WithMessage("*cannot handle activity type*");
    }
}
