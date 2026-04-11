using MediatR;

namespace MarathonTraining.Application.Strava.Connect;

public record ConnectStravaCommand(string UserId, string AuthCode) : IRequest;
