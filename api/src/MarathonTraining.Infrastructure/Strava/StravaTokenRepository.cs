using MarathonTraining.Domain.Aggregates;
using MarathonTraining.Domain.Interfaces;
using MarathonTraining.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MarathonTraining.Infrastructure.Strava;

public sealed class StravaTokenRepository(AppDbContext dbContext) : IStravaTokenRepository
{
    public Task<StravaConnection?> GetByAthleteIdAsync(Guid athleteProfileId, CancellationToken cancellationToken = default)
        => dbContext.StravaConnections
                   .FirstOrDefaultAsync(c => c.AthleteProfileId == athleteProfileId, cancellationToken);

    public async Task UpsertAsync(StravaConnection connection, CancellationToken cancellationToken = default)
    {
        var existing = await GetByAthleteIdAsync(connection.AthleteProfileId, cancellationToken);

        if (existing is null)
            dbContext.StravaConnections.Add(connection);
        else
            existing.Update(connection.AccessToken, connection.RefreshToken, connection.ExpiresAt);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteByAthleteIdAsync(Guid athleteProfileId, CancellationToken cancellationToken = default)
    {
        var connection = await GetByAthleteIdAsync(athleteProfileId, cancellationToken);

        if (connection is not null)
        {
            dbContext.StravaConnections.Remove(connection);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
