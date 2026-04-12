using MarathonTraining.Domain.Enums;
using MarathonTraining.Domain.ValueObjects;

namespace MarathonTraining.Domain.Services;

public record TssCalculationInputs(
    ActivityType ActivityType,
    ActivityDuration Duration,
    HeartRateReading? HeartRate,
    HeartRateZones? AthleteHrZones,
    NormalisedPower? NormalisedPower,
    FunctionalThresholdPower? Ftp,
    RpeScore? Rpe);
