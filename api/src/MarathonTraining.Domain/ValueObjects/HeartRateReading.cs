using MarathonTraining.Domain.Exceptions;

namespace MarathonTraining.Domain.ValueObjects;

public record HeartRateReading
{
    public int AverageHr { get; }
    public int MaxHr { get; }

    private HeartRateReading(int averageHr, int maxHr)
    {
        AverageHr = averageHr;
        MaxHr = maxHr;
    }

    public static HeartRateReading Create(int averageHr, int maxHr)
    {
        if (averageHr <= 0)
            throw new DomainException("Average heart rate must be greater than zero.");

        if (maxHr <= 0)
            throw new DomainException("Max heart rate must be greater than zero.");

        if (averageHr > maxHr)
            throw new DomainException("Average heart rate cannot exceed max heart rate.");

        return new HeartRateReading(averageHr, maxHr);
    }
}
