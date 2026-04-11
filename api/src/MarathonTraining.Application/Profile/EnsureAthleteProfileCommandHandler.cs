using MarathonTraining.Domain.Aggregates;
using MarathonTraining.Domain.Interfaces;
using MediatR;

namespace MarathonTraining.Application.Profile;

public sealed class EnsureAthleteProfileCommandHandler(
    IAthleteProfileRepository athleteProfileRepository)
    : IRequestHandler<EnsureAthleteProfileCommand, EnsureAthleteProfileResult>
{
    public async Task<EnsureAthleteProfileResult> Handle(
        EnsureAthleteProfileCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await athleteProfileRepository.GetByUserIdAsync(
            request.UserId, cancellationToken);

        if (existing is not null)
            return new EnsureAthleteProfileResult(WasCreated: false);

        var profile = new AthleteProfile(
            Guid.NewGuid(),
            request.UserId,
            request.DisplayName,
            DateTimeOffset.UtcNow);

        await athleteProfileRepository.AddAsync(profile, cancellationToken);

        return new EnsureAthleteProfileResult(WasCreated: true);
    }
}
