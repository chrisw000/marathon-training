using MarathonTraining.Domain.Aggregates;

namespace MarathonTraining.Domain.Interfaces;

public interface IActivityRepository
{
    Task<IReadOnlyCollection<Activity>> GetByTrainingWeekIdAsync(Guid trainingWeekId, CancellationToken cancellationToken = default);
    Task AddAsync(Activity activity, CancellationToken cancellationToken = default);
}
