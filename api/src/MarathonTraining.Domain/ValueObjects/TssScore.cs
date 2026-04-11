using MarathonTraining.Domain.Exceptions;

namespace MarathonTraining.Domain.ValueObjects;

public record TssScore
{
    public decimal Value { get; }

    private TssScore(decimal value)
    {
        Value = value;
    }

    public static TssScore Create(decimal value)
    {
        if (value < 0)
            throw new DomainException("TSS score cannot be negative.");

        return new TssScore(value);
    }
}
