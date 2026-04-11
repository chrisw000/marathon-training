using MarathonTraining.Domain.Aggregates;

namespace MarathonTraining.Domain.Interfaces;

public interface ITrainingWeekRepository
{
    Task<TrainingWeek?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TrainingWeek?> GetByAthleteAndDateAsync(Guid athleteId, DateOnly weekStartDate, CancellationToken cancellationToken = default);
    Task AddAsync(TrainingWeek trainingWeek, CancellationToken cancellationToken = default);
    Task UpdateAsync(TrainingWeek trainingWeek, CancellationToken cancellationToken = default);
}
