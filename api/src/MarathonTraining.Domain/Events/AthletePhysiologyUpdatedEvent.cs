namespace MarathonTraining.Domain.Events;

public record AthletePhysiologyUpdatedEvent(
    Guid AthleteId,
    int Watts,
    int RestingHr,
    int MaxHr,
    int ThresholdHr);
