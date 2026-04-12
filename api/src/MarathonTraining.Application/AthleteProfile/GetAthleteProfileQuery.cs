using MarathonTraining.Application.Common.Exceptions;
using MarathonTraining.Application.Common.Interfaces;
using MarathonTraining.Domain.Interfaces;
using MediatR;

namespace MarathonTraining.Application.Athlete;

public record GetAthleteProfileQuery : IRequest<AthleteProfileResponse>;

public record AthleteProfileResponse(
    Guid Id,
    string DisplayName,
    int? RestingHr,
    int? MaxHr,
    int? ThresholdHr,
    int? FtpWatts,
    string CurrentPhase,
    bool HasStravaConnected,
    DateTimeOffset? LastSyncedAt);

public sealed class GetAthleteProfileQueryHandler(
    IAthleteProfileRepository repository,
    ICurrentUserService currentUser)
    : IRequestHandler<GetAthleteProfileQuery, AthleteProfileResponse>
{
    public async Task<AthleteProfileResponse> Handle(
        GetAthleteProfileQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await repository.GetByUserIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException($"No athlete profile found for user '{currentUser.UserId}'.");

        return new AthleteProfileResponse(
            Id: profile.Id,
            DisplayName: profile.DisplayName,
            RestingHr: profile.HeartRateZones?.RestingHr,
            MaxHr: profile.HeartRateZones?.MaxHr,
            ThresholdHr: profile.HeartRateZones?.ThresholdHr,
            FtpWatts: profile.Ftp?.Watts,
            CurrentPhase: profile.CurrentPhase.ToString(),
            HasStravaConnected: profile.StravaConnection is not null,
            LastSyncedAt: profile.LastSyncedAt);
    }
}
