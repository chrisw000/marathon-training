using MarathonTraining.Domain.ValueObjects;

namespace MarathonTraining.Domain.Services;

public interface ITssCalculationService
{
    TssScore Calculate(TssCalculationInputs inputs);
}
