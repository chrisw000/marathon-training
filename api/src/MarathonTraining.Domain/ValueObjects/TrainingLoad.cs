using MarathonTraining.Domain.Exceptions;

namespace MarathonTraining.Domain.ValueObjects;

public record TrainingLoad
{
    public decimal AcuteTrainingLoad { get; }
    public decimal ChronicTrainingLoad { get; }
    public decimal TrainingStressBalance { get; }
    public DateOnly Date { get; }

    public bool IsOvertrainingWarning => TrainingStressBalance < -30 && TrainingStressBalance >= -50;
    public bool IsOvertrainingDanger  => TrainingStressBalance < -50;
    public bool IsRaceReady           => TrainingStressBalance >= 0 && TrainingStressBalance <= 25;
    public bool IsProductive          => TrainingStressBalance >= -30 && TrainingStressBalance < 0;

    public string FormDescription =>
        IsOvertrainingDanger  ? "Danger"      :
        IsOvertrainingWarning ? "Warning"     :
        IsProductive          ? "Productive"  :
        IsRaceReady           ? "Race ready"  :
        "Very fresh";

    private TrainingLoad(decimal atl, decimal ctl, decimal tsb, DateOnly date)
    {
        AcuteTrainingLoad = atl;
        ChronicTrainingLoad = ctl;
        TrainingStressBalance = tsb;
        Date = date;
    }

    public static TrainingLoad Create(decimal atl, decimal ctl, decimal tsb, DateOnly date)
    {
        if (atl < 0)
            throw new DomainException("Acute training load cannot be negative.");

        if (ctl < 0)
            throw new DomainException("Chronic training load cannot be negative.");

        return new TrainingLoad(atl, ctl, tsb, date);
    }
}
