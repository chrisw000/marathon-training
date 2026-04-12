using MarathonTraining.Domain.Exceptions;
using MarathonTraining.Domain.ValueObjects;

namespace MarathonTraining.Domain.Services;

public sealed class TssCalculationService(IEnumerable<ITssCalculator> calculators) : ITssCalculationService
{
    public TssScore Calculate(TssCalculationInputs inputs)
    {
        foreach (var calculator in calculators)
        {
            try
            {
                return calculator.Calculate(inputs);
            }
            catch (DomainException ex) when (ex.Message.Contains("cannot handle activity type"))
            {
                // This calculator does not handle this ActivityType — try the next one.
                continue;
            }
            catch (DomainException ex)
            {
                // Correct calculator, but domain rule violated (e.g. missing data).
                throw new DomainException(
                    $"TSS calculation failed for {inputs.ActivityType}: {ex.Message}", ex);
            }
        }

        throw new DomainException(
            $"No TSS calculator registered for activity type '{inputs.ActivityType}'.");
    }
}
