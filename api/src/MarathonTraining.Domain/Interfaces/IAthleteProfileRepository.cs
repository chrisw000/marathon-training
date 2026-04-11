using MarathonTraining.Domain.Aggregates;

namespace MarathonTraining.Domain.Interfaces;

public interface IAthleteProfileRepository
{
    Task<AthleteProfile?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task AddAsync(AthleteProfile profile, CancellationToken cancellationToken = default);
    Task UpdateAsync(AthleteProfile profile, CancellationToken cancellationToken = default);
}
