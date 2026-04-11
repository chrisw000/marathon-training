using MarathonTraining.Domain.Exceptions;
using MarathonTraining.Domain.Interfaces;
using MediatR;

namespace MarathonTraining.Application.Strava.Disconnect;

public sealed class DisconnectStravaCommandHandler(
    IAthleteProfileRepository athleteProfileRepository,
    IStravaTokenRepository stravaTokenRepository) : IRequestHandler<DisconnectStravaCommand>
{
    public async Task Handle(DisconnectStravaCommand request, CancellationToken cancellationToken)
    {
        var profile = await athleteProfileRepository.GetByUserIdAsync(request.UserId, cancellationToken)
            ?? throw new DomainException($"No athlete profile found for user '{request.UserId}'.");

        var connection = await stravaTokenRepository.GetByAthleteIdAsync(profile.Id, cancellationToken)
            ?? throw new DomainException($"Athlete '{profile.Id}' does not have a Strava connection to disconnect.");

        await stravaTokenRepository.DeleteByAthleteIdAsync(profile.Id, cancellationToken);
    }
}
