using MarathonTraining.Domain.Enums;

namespace MarathonTraining.Domain.Events;

public record TrainingPhaseChangedEvent(
    Guid AthleteId,
    TrainingPhase OldPhase,
    TrainingPhase NewPhase);
