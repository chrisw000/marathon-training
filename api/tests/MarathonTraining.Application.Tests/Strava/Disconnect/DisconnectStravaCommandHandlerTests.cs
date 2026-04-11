using MarathonTraining.Application.Strava.Disconnect;
using MarathonTraining.Application.Tests.Fakers;
using MarathonTraining.Domain.Aggregates;
using MarathonTraining.Domain.Exceptions;
using MarathonTraining.Domain.Interfaces;
using NSubstitute;

namespace MarathonTraining.Application.Tests.Strava.Disconnect;

public sealed class DisconnectStravaCommandHandlerTests
{
    private readonly IAthleteProfileRepository _athleteProfileRepository =
        Substitute.For<IAthleteProfileRepository>();

    private readonly IStravaTokenRepository _stravaTokenRepository =
        Substitute.For<IStravaTokenRepository>();

    private readonly DisconnectStravaCommandHandler _sut;

    public DisconnectStravaCommandHandlerTests()
    {
        _sut = new DisconnectStravaCommandHandler(_athleteProfileRepository, _stravaTokenRepository);
    }

    [Fact]
    public async Task Handle_ConnectedAthlete_DeletesConnection()
    {
        var profile = StravaDomainFakers.AthleteProfile();
        var connection = StravaDomainFakers.StravaConnection(athleteProfileId: profile.Id);
        var command = new DisconnectStravaCommand(profile.UserId);

        _athleteProfileRepository
            .GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>())
            .Returns(profile);

        _stravaTokenRepository
            .GetByAthleteIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(connection);

        await _sut.Handle(command, CancellationToken.None);

        await _stravaTokenRepository.Received(1).DeleteByAthleteIdAsync(
            profile.Id,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoConnection_ThrowsDomainException()
    {
        var profile = StravaDomainFakers.AthleteProfile();
        var command = new DisconnectStravaCommand(profile.UserId);

        _athleteProfileRepository
            .GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>())
            .Returns(profile);

        _stravaTokenRepository
            .GetByAthleteIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns((StravaConnection?)null);

        Func<Task> act = () => _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*does not have a Strava connection*");

        await _stravaTokenRepository.DidNotReceive().DeleteByAthleteIdAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }
}
