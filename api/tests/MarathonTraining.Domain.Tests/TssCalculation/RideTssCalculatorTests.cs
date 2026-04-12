using MarathonTraining.Domain.Enums;
using MarathonTraining.Domain.Exceptions;
using MarathonTraining.Domain.Services;
using MarathonTraining.Domain.ValueObjects;

namespace MarathonTraining.Domain.Tests.TssCalculation;

// Profile: FTP=220w, HR zones for fallback tests
public sealed class RideTssCalculatorTests
{
    private static readonly FunctionalThresholdPower Ftp = FunctionalThresholdPower.Create(220);
    private static readonly HeartRateZones Zones = HeartRateZones.Create(50, 185, 162);
    private readonly RideTssCalculator _sut = new();

    private static TssCalculationInputs PowerInputs(int durationSeconds, int npWatts) =>
        new(
            ActivityType: ActivityType.Ride,
            Duration: ActivityDuration.Create(durationSeconds),
            HeartRate: null,
            AthleteHrZones: null,
            NormalisedPower: NormalisedPower.Create(npWatts),
            Ftp: Ftp,
            Rpe: null);

    [Fact]
    public void Calculate_OneHourAtFtp_TssEquals100()
    {
        var result = _sut.Calculate(PowerInputs(3600, 220));

        ((double)result.Value).Should().BeApproximately(100.0, 0.1);
    }

    [Fact]
    public void Calculate_90MinEndurance_Np176_TssApprox96()
    {
        var result = _sut.Calculate(PowerInputs(5400, 176));

        ((double)result.Value).Should().BeApproximately(96.0, 0.5);
    }

    [Fact]
    public void Calculate_45MinHardRace_Np242_TssApprox90_8()
    {
        var result = _sut.Calculate(PowerInputs(2700, 242));

        ((double)result.Value).Should().BeApproximately(90.8, 0.5);
    }

    [Fact]
    public void Calculate_2HrSteadyRide_Np165_TssApprox112_5()
    {
        // TSS = t_hours × (NP/FTP)² × 100 = 2 × (165/220)² × 100 = 2 × 0.5625 × 100 = 112.5
        var result = _sut.Calculate(PowerInputs(7200, 165));

        ((double)result.Value).Should().BeApproximately(112.5, 0.5);
    }

    [Fact]
    public void Calculate_30MinRecovery_Np132_TssApprox18()
    {
        // TSS = t_hours × (NP/FTP)² × 100 = 0.5 × (132/220)² × 100 = 0.5 × 0.36 × 100 = 18.0
        var result = _sut.Calculate(PowerInputs(1800, 132));

        ((double)result.Value).Should().BeApproximately(18.0, 0.5);
    }

    [Fact]
    public void Calculate_NoPowerButHrPresent_FallsBackToHrTss_ReturnsNonZero()
    {
        var inputs = new TssCalculationInputs(
            ActivityType: ActivityType.Ride,
            Duration: ActivityDuration.Create(3600),
            HeartRate: HeartRateReading.Create(145, 185),
            AthleteHrZones: Zones,
            NormalisedPower: null,
            Ftp: null,
            Rpe: null);

        var result = _sut.Calculate(inputs);

        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Calculate_NoPowerAndNoHr_ThrowsDomainException()
    {
        var inputs = new TssCalculationInputs(
            ActivityType: ActivityType.Ride,
            Duration: ActivityDuration.Create(3600),
            HeartRate: null,
            AthleteHrZones: null,
            NormalisedPower: null,
            Ftp: null,
            Rpe: null);

        var act = () => _sut.Calculate(inputs);

        act.Should().Throw<DomainException>().WithMessage("*power*heart rate*");
    }

    [Fact]
    public void Calculate_WrongActivityType_ThrowsDomainException()
    {
        var inputs = new TssCalculationInputs(
            ActivityType: ActivityType.Run,
            Duration: ActivityDuration.Create(3600),
            HeartRate: null,
            AthleteHrZones: null,
            NormalisedPower: NormalisedPower.Create(220),
            Ftp: Ftp,
            Rpe: null);

        var act = () => _sut.Calculate(inputs);

        act.Should().Throw<DomainException>().WithMessage("*cannot handle activity type*");
    }
}
