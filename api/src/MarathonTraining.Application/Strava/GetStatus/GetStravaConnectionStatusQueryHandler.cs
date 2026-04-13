using MarathonTraining.Domain.Interfaces;
using MediatR;

namespace MarathonTraining.Application.Strava.GetStatus;

public sealed class GetStravaConnectionStatusQueryHandler(
    IAthleteProfileRepository athleteProfileRepository,
    IStravaTokenRepository stravaTokenRepository) : IRequestHandler<GetStravaConnectionStatusQuery, StravaConnectionStatusDto>
{
    public async Task<StravaConnectionStatusDto> Handle(
        GetStravaConnectionStatusQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await athleteProfileRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        if (profile is null)
            return new StravaConnectionStatusDto(false, null, null);

        var connection = await stravaTokenRepository.GetByAthleteIdAsync(profile.Id, cancellationToken);

        if (connection is null)
            return new StravaConnectionStatusDto(false, null, null);

        // A connection record exists. The access token may be expired but the sync
        // handler refreshes it automatically — report as connected in both cases.
        return new StravaConnectionStatusDto(true, connection.StravaAthleteId, connection.ExpiresAt);
    }
}
