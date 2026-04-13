using MarathonTraining.Domain.Aggregates;

namespace MarathonTraining.Domain.Interfaces;

public interface IAthleteProfileRepository
{
    Task<AthleteProfile?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<AthleteProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    /// <returns>True if the profile was inserted; false if a concurrent request already inserted it.</returns>
    Task<bool> AddAsync(AthleteProfile profile, CancellationToken cancellationToken = default);
    Task UpdateAsync(AthleteProfile profile, CancellationToken cancellationToken = default);
}
