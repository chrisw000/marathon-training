using MarathonTraining.Domain.Enums;
using MarathonTraining.Domain.Exceptions;
using MarathonTraining.Domain.ValueObjects;

namespace MarathonTraining.Domain.Services;

public sealed class RideTssCalculator : ITssCalculator
{
    public TssScore Calculate(TssCalculationInputs inputs)
    {
        if (inputs.ActivityType != ActivityType.Ride)
            throw new DomainException($"RideTssCalculator cannot handle activity type '{inputs.ActivityType}'.");

        decimal rawTss;

        if (inputs.NormalisedPower is not null && inputs.Ftp is not null)
        {
            rawTss = CalculatePowerTss(inputs);
        }
        else if (inputs.HeartRate is not null && inputs.AthleteHrZones is not null)
        {
            rawTss = CalculateHrTss(inputs);
        }
        else
        {
            throw new DomainException(
                "Cannot calculate ride TSS: neither power data (NP + FTP) nor heart rate data is available.");
        }

        return TssScore.Create(Math.Round(rawTss, 1));
    }

    private static decimal CalculatePowerTss(TssCalculationInputs inputs)
    {
        var np = inputs.NormalisedPower!;
        var ftp = inputs.Ftp!;

        double intensityFactor = np.Watts / (double)ftp.Watts;
        double rawTss = (inputs.Duration.Seconds * np.Watts * intensityFactor)
                        / (ftp.Watts * 3600.0) * 100.0;

        return (decimal)rawTss;
    }

    private static decimal CalculateHrTss(TssCalculationInputs inputs)
    {
        var hr = inputs.HeartRate!;
        var zones = inputs.AthleteHrZones!;

        double hrReserve = (hr.AverageHr - zones.RestingHr)
                           / (double)(zones.MaxHr - zones.RestingHr);

        double thresholdReserve = (zones.ThresholdHr - zones.RestingHr)
                                  / (double)(zones.MaxHr - zones.RestingHr);

        double trimp = inputs.Duration.Hours
                       * hrReserve
                       * 0.64
                       * Math.Exp(1.92 * hrReserve);

        double thresholdTrimp = 1.0
                                * thresholdReserve
                                * 0.64
                                * Math.Exp(1.92 * thresholdReserve);

        if (thresholdTrimp == 0)
            return 0m;

        double rawTss = (trimp / thresholdTrimp) * 100.0;
        return (decimal)rawTss;
    }
}
