using MarathonTraining.Domain.ValueObjects;

namespace MarathonTraining.Domain.Services;

public interface ITssCalculator
{
    TssScore Calculate(TssCalculationInputs inputs);
}
