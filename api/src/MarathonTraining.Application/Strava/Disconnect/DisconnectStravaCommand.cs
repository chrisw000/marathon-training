using MediatR;

namespace MarathonTraining.Application.Strava.Disconnect;

public record DisconnectStravaCommand(string UserId) : IRequest;
