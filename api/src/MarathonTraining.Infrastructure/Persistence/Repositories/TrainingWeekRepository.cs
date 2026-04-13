using MarathonTraining.Domain.Aggregates;
using MarathonTraining.Domain.Interfaces;
using MarathonTraining.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MarathonTraining.Infrastructure.Persistence.Repositories;

public sealed class TrainingWeekRepository(AppDbContext dbContext) : ITrainingWeekRepository
{
    public Task<TrainingWeek?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.TrainingWeeks
            .Include(w => w.Activities)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public Task<TrainingWeek?> GetByAthleteAndDateAsync(
        Guid athleteId, DateOnly weekStartDate, CancellationToken cancellationToken = default)
        => dbContext.TrainingWeeks
            .Include(w => w.Activities)
            .FirstOrDefaultAsync(
                w => w.AthleteId == athleteId && w.WeekStartDate == weekStartDate,
                cancellationToken);

    public async Task AddAsync(TrainingWeek trainingWeek, CancellationToken cancellationToken = default)
    {
        dbContext.TrainingWeeks.Add(trainingWeek);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TrainingWeek trainingWeek, CancellationToken cancellationToken = default)
    {
        var entry = dbContext.Entry(trainingWeek);
        if (entry.State == EntityState.Detached)
        {
            // Entity not tracked — use Update() to attach the full graph.
            // This is safe only when no new child entities are in the graph
            // (they'd be marked Modified instead of Added). The detached path
            // is only hit when the TrainingWeek was never queried in this
            // DbContext lifetime (i.e. it was constructed by the caller).
            dbContext.TrainingWeeks.Update(trainingWeek);
        }
        else
        {
            // Entity IS tracked (Unchanged after Add or query). Explicitly ensure any
            // newly-created activities are marked as Added (INSERT).
            //
            // When a new Activity is added to the collection of a tracked TrainingWeek,
            // calling dbContext.Entry(activity) triggers DetectChanges, which discovers the
            // activity via the navigation property. Because the activity has a non-default
            // Guid key (client-generated), EF Core cannot determine whether it is new or
            // existing and conservatively marks it Modified. A subsequent SaveChangesAsync
            // then issues UPDATE ... WHERE Id = @id, which affects 0 rows and throws
            // DbUpdateConcurrencyException.
            //
            // Setting the state explicitly to Added forces EF Core to INSERT instead.
            // Activities that were already tracked as Unchanged (loaded from DB) are
            // unaffected — their state remains Unchanged.
            foreach (var activity in trainingWeek.Activities)
            {
                var activityEntry = dbContext.Entry(activity);
                if (activityEntry.State is EntityState.Detached or EntityState.Modified)
                    activityEntry.State = EntityState.Added;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
