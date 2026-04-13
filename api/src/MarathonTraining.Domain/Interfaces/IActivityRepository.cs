using MarathonTraining.Domain.Aggregates;

namespace MarathonTraining.Domain.Interfaces;

public interface IActivityRepository
{
    Task<Activity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Activity?> GetByStravaIdAsync(long stravaActivityId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Activity>> GetByTrainingWeekIdAsync(Guid trainingWeekId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Activity>> GetAllByAthleteIdAsync(Guid athleteProfileId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Activity>> GetByAthleteAndDateRangeAsync(Guid athleteProfileId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    Task AddAsync(Activity activity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Activity activity, CancellationToken cancellationToken = default);
    Task UpdateRangeAsync(IEnumerable<Activity> activities, CancellationToken cancellationToken = default);
}
