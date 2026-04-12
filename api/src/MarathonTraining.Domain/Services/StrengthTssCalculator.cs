using MarathonTraining.Domain.Enums;
using MarathonTraining.Domain.Exceptions;
using MarathonTraining.Domain.ValueObjects;

namespace MarathonTraining.Domain.Services;

public sealed class StrengthTssCalculator : ITssCalculator
{
    public TssScore Calculate(TssCalculationInputs inputs)
    {
        if (inputs.ActivityType != ActivityType.Strength)
            throw new DomainException($"StrengthTssCalculator cannot handle activity type '{inputs.ActivityType}'.");

        if (inputs.Rpe is null)
            throw new DomainException("Cannot calculate strength TSS: RPE score is required.");

        double rawTss = inputs.Duration.Minutes / 60.0
                        * (inputs.Rpe.Value / 10.0)
                        * 100.0
                        * 0.55;

        return TssScore.Create(Math.Round((decimal)rawTss, 2));
    }
}
