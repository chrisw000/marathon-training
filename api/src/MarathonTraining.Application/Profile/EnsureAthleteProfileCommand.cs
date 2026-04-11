using MarathonTraining.Domain.Interfaces;
using MediatR;

namespace MarathonTraining.Application.Profile;

public sealed record EnsureAthleteProfileCommand(string UserId, string DisplayName)
    : IRequest<EnsureAthleteProfileResult>;

public sealed record EnsureAthleteProfileResult(bool WasCreated);
