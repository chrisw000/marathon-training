using MarathonTraining.Domain.Aggregates;
using MarathonTraining.Domain.Interfaces;
using MarathonTraining.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MarathonTraining.Infrastructure.Persistence.Repositories;

public sealed class ActivityRepository(AppDbContext dbContext) : IActivityRepository
{
    public Task<Activity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.Activities.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<Activity?> GetByStravaIdAsync(long stravaActivityId, CancellationToken cancellationToken = default)
        => dbContext.Activities.FirstOrDefaultAsync(a => a.StravaActivityId == stravaActivityId, cancellationToken);

    public async Task<IReadOnlyCollection<Activity>> GetByTrainingWeekIdAsync(
        Guid trainingWeekId, CancellationToken cancellationToken = default)
        => await dbContext.Activities
            .Where(a => a.TrainingWeekId == trainingWeekId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Activity>> GetAllByAthleteIdAsync(
        Guid athleteProfileId, CancellationToken cancellationToken = default)
        => await dbContext.Activities
            .Where(a => a.AthleteProfileId == athleteProfileId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Activity>> GetByAthleteAndDateRangeAsync(
        Guid athleteProfileId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc   = to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        return await dbContext.Activities
            .Where(a => a.AthleteProfileId == athleteProfileId
                     && a.StartedAt >= fromUtc
                     && a.StartedAt <= toUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Activity activity, CancellationToken cancellationToken = default)
    {
        dbContext.Activities.Add(activity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Activity activity, CancellationToken cancellationToken = default)
    {
        dbContext.Activities.Update(activity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateRangeAsync(
        IEnumerable<Activity> activities, CancellationToken cancellationToken = default)
    {
        dbContext.Activities.UpdateRange(activities);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
