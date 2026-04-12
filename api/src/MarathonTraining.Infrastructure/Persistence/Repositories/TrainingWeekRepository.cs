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
        dbContext.TrainingWeeks.Update(trainingWeek);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
