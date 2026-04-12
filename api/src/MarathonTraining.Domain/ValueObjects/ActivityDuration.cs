using MarathonTraining.Domain.Exceptions;

namespace MarathonTraining.Domain.ValueObjects;

public record ActivityDuration
{
    public int Seconds { get; }
    public double Hours => Seconds / 3600.0;
    public double Minutes => Seconds / 60.0;

    private ActivityDuration(int seconds)
    {
        Seconds = seconds;
    }

    public static ActivityDuration Create(int seconds)
    {
        if (seconds <= 0)
            throw new DomainException("Activity duration must be greater than zero seconds.");

        return new ActivityDuration(seconds);
    }
}
