using MarathonTraining.Domain.Aggregates;
using MarathonTraining.Domain.Exceptions;
using MarathonTraining.Domain.Interfaces;
using MediatR;

namespace MarathonTraining.Application.Strava.Connect;

public sealed class ConnectStravaCommandHandler(
    IAthleteProfileRepository athleteProfileRepository,
    IStravaTokenRepository stravaTokenRepository,
    IStravaTokenService stravaTokenService) : IRequestHandler<ConnectStravaCommand>
{
    public async Task Handle(ConnectStravaCommand request, CancellationToken cancellationToken)
    {
        var profile = await athleteProfileRepository.GetByUserIdAsync(request.UserId, cancellationToken)
            ?? throw new DomainException($"No athlete profile found for user '{request.UserId}'.");

        StravaTokenResponse tokens;
        try
        {
            tokens = await stravaTokenService.ExchangeCodeAsync(request.AuthCode, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new DomainException("Failed to exchange Strava authorisation code.", ex);
        }

        var connection = new StravaConnection(
            profile.Id,
            tokens.StravaAthleteId,
            tokens.AccessToken,
            tokens.RefreshToken,
            tokens.ExpiresAt);

        await stravaTokenRepository.UpsertAsync(connection, cancellationToken);
    }
}
