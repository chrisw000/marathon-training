using MarathonTraining.Application.Strava.GetStatus;
using MarathonTraining.Application.Tests.Fakers;
using MarathonTraining.Domain.Aggregates;
using MarathonTraining.Domain.Interfaces;
using NSubstitute;

namespace MarathonTraining.Application.Tests.Strava.GetStatus;

public sealed class GetStravaConnectionStatusQueryHandlerTests
{
    private readonly IAthleteProfileRepository _athleteProfileRepository =
        Substitute.For<IAthleteProfileRepository>();

    private readonly IStravaTokenRepository _stravaTokenRepository =
        Substitute.For<IStravaTokenRepository>();

    private readonly GetStravaConnectionStatusQueryHandler _sut;

    public GetStravaConnectionStatusQueryHandlerTests()
    {
        _sut = new GetStravaConnectionStatusQueryHandler(_athleteProfileRepository, _stravaTokenRepository);
    }

    [Fact]
    public async Task Handle_ValidToken_ReturnsConnectedWithExpiry()
    {
        var profile = StravaDomainFakers.AthleteProfile();
        var expiresAt = DateTimeOffset.UtcNow.AddHours(2);
        var connection = StravaDomainFakers.StravaConnection(athleteProfileId: profile.Id, expiresAt: expiresAt);
        var query = new GetStravaConnectionStatusQuery(profile.UserId);

        _athleteProfileRepository
            .GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>())
            .Returns(profile);

        _stravaTokenRepository
            .GetByAthleteIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(connection);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsConnected.Should().BeTrue();
        result.StravaAthleteId.Should().Be(connection.StravaAthleteId);
        result.ExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public async Task Handle_NoToken_ReturnsNotConnected()
    {
        var profile = StravaDomainFakers.AthleteProfile();
        var query = new GetStravaConnectionStatusQuery(profile.UserId);

        _athleteProfileRepository
            .GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>())
            .Returns(profile);

        _stravaTokenRepository
            .GetByAthleteIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns((StravaConnection?)null);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsConnected.Should().BeFalse();
        result.StravaAthleteId.Should().BeNull();
        result.ExpiresAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ExpiredToken_ReturnsConnected()
    {
        // Strava access tokens expire every 6 hours. The sync handler refreshes them
        // automatically, so an expired-but-refreshable token is still "connected".
        var profile = StravaDomainFakers.AthleteProfile();
        var expiresAt = DateTimeOffset.UtcNow.AddHours(-1);
        var connection = StravaDomainFakers.StravaConnection(athleteProfileId: profile.Id, expiresAt: expiresAt);
        var query = new GetStravaConnectionStatusQuery(profile.UserId);

        _athleteProfileRepository
            .GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>())
            .Returns(profile);

        _stravaTokenRepository
            .GetByAthleteIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(connection);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsConnected.Should().BeTrue();
        result.StravaAthleteId.Should().Be(connection.StravaAthleteId);
        result.ExpiresAt.Should().Be(expiresAt);
    }
}
