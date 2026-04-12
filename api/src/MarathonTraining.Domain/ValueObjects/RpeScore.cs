using MarathonTraining.Domain.Exceptions;

namespace MarathonTraining.Domain.ValueObjects;

public record RpeScore
{
    public int Value { get; }

    private RpeScore(int value)
    {
        Value = value;
    }

    public static RpeScore Create(int value)
    {
        if (value < 1 || value > 10)
            throw new DomainException("RPE score must be between 1 and 10.");

        return new RpeScore(value);
    }
}
