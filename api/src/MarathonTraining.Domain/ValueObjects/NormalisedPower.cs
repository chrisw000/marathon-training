using MarathonTraining.Domain.Exceptions;

namespace MarathonTraining.Domain.ValueObjects;

public record NormalisedPower
{
    public int Watts { get; }

    private NormalisedPower(int watts)
    {
        Watts = watts;
    }

    public static NormalisedPower Create(int watts)
    {
        if (watts <= 0)
            throw new DomainException("Normalised power must be greater than zero.");

        if (watts >= 2500)
            throw new DomainException("Normalised power must be less than 2500 watts.");

        return new NormalisedPower(watts);
    }
}
