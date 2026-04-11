using MarathonTraining.Domain.Aggregates;

namespace MarathonTraining.Domain.Interfaces;

public interface IStravaTokenRepository
{
    Task<StravaConnection?> GetByAthleteIdAsync(Guid athleteProfileId, CancellationToken cancellationToken = default);
    Task UpsertAsync(StravaConnection connection, CancellationToken cancellationToken = default);
    Task DeleteByAthleteIdAsync(Guid athleteProfileId, CancellationToken cancellationToken = default);
}
