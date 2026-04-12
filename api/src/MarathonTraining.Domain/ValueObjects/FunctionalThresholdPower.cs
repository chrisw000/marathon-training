using MarathonTraining.Domain.Exceptions;

namespace MarathonTraining.Domain.ValueObjects;

public record FunctionalThresholdPower
{
    public int Watts { get; }

    private FunctionalThresholdPower(int watts)
    {
        Watts = watts;
    }

    public static FunctionalThresholdPower Create(int watts)
    {
        if (watts <= 0)
            throw new DomainException("FTP must be greater than zero.");

        if (watts >= 600)
            throw new DomainException("FTP must be less than 600 watts.");

        return new FunctionalThresholdPower(watts);
    }
}
