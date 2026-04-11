using MediatR;

namespace MarathonTraining.Application.Strava.GetStatus;

public record GetStravaConnectionStatusQuery(string UserId) : IRequest<StravaConnectionStatusDto>;
