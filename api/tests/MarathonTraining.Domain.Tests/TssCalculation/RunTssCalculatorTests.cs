using MarathonTraining.Domain.Enums;
using MarathonTraining.Domain.Exceptions;
using MarathonTraining.Domain.Services;
using MarathonTraining.Domain.ValueObjects;

namespace MarathonTraining.Domain.Tests.TssCalculation;

// Profile used in all tests: RestingHr=50, MaxHr=185, ThresholdHr=162
public sealed class RunTssCalculatorTests
{
    private static readonly HeartRateZones Zones = HeartRateZones.Create(50, 185, 162);
    private readonly RunTssCalculator _sut = new();

    private static TssCalculationInputs Inputs(
        int durationSeconds,
        int? avgHr = null,
        int? maxHr = null,
        HeartRateZones? zones = null) =>
        new(
            ActivityType: ActivityType.Run,
            Duration: ActivityDuration.Create(durationSeconds),
            HeartRate: avgHr.HasValue && maxHr.HasValue
                ? HeartRateReading.Create(avgHr.Value, maxHr.Value)
                : null,
            AthleteHrZones: zones ?? (avgHr.HasValue ? Zones : null),
            NormalisedPower: null,
            Ftp: null,
            Rpe: null);

    [Fact]
    public void Calculate_OneHourAtThresholdHr162_TssApprox100()
    {
        var result = _sut.Calculate(Inputs(3600, avgHr: 162, maxHr: 185));

        ((double)result.Value).Should().BeApproximately(100.0, 0.5);
    }

    [Fact]
    public void Calculate_Easy60MinAvgHr135_TssApprox51_7()
    {
        // hrReserve = (135-50)/(185-50) = 0.6296; thresholdReserve = (162-50)/(185-50) = 0.8296
        var result = _sut.Calculate(Inputs(3600, avgHr: 135, maxHr: 185));

        ((double)result.Value).Should().BeApproximately(51.7, 1.0);
    }

    [Fact]
    public void Calculate_Hard45MinAvgHr158_TssApprox68_3()
    {
        // hrReserve = (158-50)/(185-50) = 0.8, thresholdReserve = (162-50)/(185-50) ≈ 0.8296
        var result = _sut.Calculate(Inputs(2700, avgHr: 158, maxHr: 185));

        ((double)result.Value).Should().BeApproximately(68.3, 1.0);
    }

    [Fact]
    public void Calculate_LongEasy2HrAvgHr130_TssApprox90_6()
    {
        // hrReserve = (130-50)/(185-50) = 0.5926; 2 hours doubles the TRIMP vs 1-hour baseline
        var result = _sut.Calculate(Inputs(7200, avgHr: 130, maxHr: 185));

        ((double)result.Value).Should().BeApproximately(90.6, 1.0);
    }

    [Fact]
    public void Calculate_Short20MinRecoveryAvgHr120_TssApprox11_5()
    {
        // hrReserve = (120-50)/(185-50) = 0.5185
        var result = _sut.Calculate(Inputs(1200, avgHr: 120, maxHr: 185));

        ((double)result.Value).Should().BeApproximately(11.5, 0.5);
    }

    [Fact]
    public void Calculate_NoHrData_FallsBackToPaceEstimate_ReturnsNonZero()
    {
        var inputs = new TssCalculationInputs(
            ActivityType: ActivityType.Run,
            Duration: ActivityDuration.Create(3600),
            HeartRate: null,
            AthleteHrZones: null,
            NormalisedPower: null,
            Ftp: null,
            Rpe: null);

        var result = _sut.Calculate(inputs);

        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Calculate_WrongActivityType_ThrowsDomainException()
    {
        var inputs = new TssCalculationInputs(
            ActivityType: ActivityType.Ride,
            Duration: ActivityDuration.Create(3600),
            HeartRate: HeartRateReading.Create(150, 185),
            AthleteHrZones: Zones,
            NormalisedPower: null,
            Ftp: null,
            Rpe: null);

        var act = () => _sut.Calculate(inputs);

        act.Should().Throw<DomainException>().WithMessage("*cannot handle activity type*");
    }

    [Fact]
    public void Calculate_HrReserveOfZeroAvgEqualsResting_ReturnsNearZeroNotError()
    {
        // avgHr == restingHr → hr_reserve = 0 → TRIMP = 0 → TSS = 0
        // maxHr must be >= avgHr per HeartRateReading.Create rules
        var inputs = new TssCalculationInputs(
            ActivityType: ActivityType.Run,
            Duration: ActivityDuration.Create(3600),
            HeartRate: HeartRateReading.Create(50, 185),
            AthleteHrZones: Zones,
            NormalisedPower: null,
            Ftp: null,
            Rpe: null);

        var result = _sut.Calculate(inputs);

        result.Value.Should().BeGreaterThanOrEqualTo(0m);
        result.Value.Should().BeLessThan(1m);
    }
}
