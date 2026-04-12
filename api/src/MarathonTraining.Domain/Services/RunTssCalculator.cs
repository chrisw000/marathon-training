using MarathonTraining.Domain.Enums;
using MarathonTraining.Domain.Exceptions;
using MarathonTraining.Domain.ValueObjects;

namespace MarathonTraining.Domain.Services;

public sealed class RunTssCalculator : ITssCalculator
{
    public TssScore Calculate(TssCalculationInputs inputs)
    {
        if (inputs.ActivityType != ActivityType.Run)
            throw new DomainException($"RunTssCalculator cannot handle activity type '{inputs.ActivityType}'.");

        decimal rawTss;

        if (inputs.HeartRate is not null && inputs.AthleteHrZones is not null)
        {
            rawTss = CalculateHrTss(inputs);
        }
        else
        {
            // Fallback: pace-based estimate — TSS ≈ duration_hours × 60
            // Conservative estimate assuming moderate effort when no HR data is available.
            rawTss = (decimal)(inputs.Duration.Hours * 60.0);
        }

        return TssScore.Create(Math.Round(rawTss, 1));
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

        // Edge case: if hr_reserve is 0 (avg == resting), TRIMP is 0 → TSS is 0
        if (thresholdTrimp == 0)
            return 0m;

        double rawTss = (trimp / thresholdTrimp) * 100.0;
        return (decimal)rawTss;
    }
}
