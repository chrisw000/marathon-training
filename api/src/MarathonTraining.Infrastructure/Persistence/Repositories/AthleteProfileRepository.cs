using MarathonTraining.Domain.Aggregates;
using MarathonTraining.Domain.Interfaces;
using MarathonTraining.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MarathonTraining.Infrastructure.Persistence.Repositories;

public sealed class AthleteProfileRepository(AppDbContext dbContext) : IAthleteProfileRepository
{
    public Task<AthleteProfile?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        => dbContext.AthleteProfiles
                   .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);

    public Task<AthleteProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.AthleteProfiles
                   .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task AddAsync(AthleteProfile profile, CancellationToken cancellationToken = default)
    {
        dbContext.AthleteProfiles.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AthleteProfile profile, CancellationToken cancellationToken = default)
    {
        dbContext.AthleteProfiles.Update(profile);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
