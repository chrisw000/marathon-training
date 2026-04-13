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

    public async Task<bool> AddAsync(AthleteProfile profile, CancellationToken cancellationToken = default)
    {
        dbContext.AthleteProfiles.Add(profile);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx
                  && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
        {
            // A concurrent request already inserted a profile for this UserId.
            // 2601 = unique index violation, 2627 = unique constraint violation.
            // Detach the entity so the DbContext remains usable for subsequent operations.
            dbContext.Entry(profile).State = EntityState.Detached;
            return false;
        }
    }

    public async Task UpdateAsync(AthleteProfile profile, CancellationToken cancellationToken = default)
    {
        dbContext.AthleteProfiles.Update(profile);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
