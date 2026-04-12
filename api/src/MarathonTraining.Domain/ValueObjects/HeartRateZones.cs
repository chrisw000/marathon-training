using MarathonTraining.Domain.Exceptions;

namespace MarathonTraining.Domain.ValueObjects;

public record HeartRateZones
{
    public int RestingHr { get; }
    public int MaxHr { get; }
    public int ThresholdHr { get; }

    private HeartRateZones(int restingHr, int maxHr, int thresholdHr)
    {
        RestingHr = restingHr;
        MaxHr = maxHr;
        ThresholdHr = thresholdHr;
    }

    public static HeartRateZones Create(int restingHr, int maxHr, int thresholdHr)
    {
        if (restingHr <= 0)
            throw new DomainException("Resting heart rate must be greater than zero.");

        if (maxHr <= 100)
            throw new DomainException("Maximum heart rate must be greater than 100.");

        if (maxHr >= 230)
            throw new DomainException("Maximum heart rate must be less than 230.");

        if (restingHr >= maxHr)
            throw new DomainException("Resting heart rate must be less than maximum heart rate.");

        if (thresholdHr <= restingHr)
            throw new DomainException("Threshold heart rate must be greater than resting heart rate.");

        if (thresholdHr >= maxHr)
            throw new DomainException("Threshold heart rate must be less than maximum heart rate.");

        return new HeartRateZones(restingHr, maxHr, thresholdHr);
    }
}
