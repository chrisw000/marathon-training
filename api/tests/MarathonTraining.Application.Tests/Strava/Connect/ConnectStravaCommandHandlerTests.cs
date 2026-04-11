using MarathonTraining.Application.Strava;
using MarathonTraining.Application.Strava.Connect;
using MarathonTraining.Application.Tests.Fakers;
using MarathonTraining.Domain.Aggregates;
using MarathonTraining.Domain.Exceptions;
using MarathonTraining.Domain.Interfaces;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace MarathonTraining.Application.Tests.Strava.Connect;

public sealed class ConnectStravaCommandHandlerTests
{
    private readonly IAthleteProfileRepository _athleteProfileRepository =
        Substitute.For<IAthleteProfileRepository>();

    private readonly IStravaTokenRepository _stravaTokenRepository =
        Substitute.For<IStravaTokenRepository>();

    private readonly IStravaTokenService _stravaTokenService =
        Substitute.For<IStravaTokenService>();

    private readonly ConnectStravaCommandHandler _sut;

    public ConnectStravaCommandHandlerTests()
    {
        _sut = new ConnectStravaCommandHandler(
            _athleteProfileRepository,
            _stravaTokenRepository,
            _stravaTokenService);
    }

    [Fact]
    public async Task Handle_ValidAuthCode_StoresTokens()
    {
        var profile = StravaDomainFakers.AthleteProfile();
        var tokenResponse = StravaDomainFakers.StravaTokenResponse();
        var command = new ConnectStravaCommand(profile.UserId, "valid-auth-code");

        _athleteProfileRepository
            .GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>())
            .Returns(profile);

        _stravaTokenService
            .ExchangeCodeAsync(command.AuthCode, Arg.Any<CancellationToken>())
            .Returns(tokenResponse);

        await _sut.Handle(command, CancellationToken.None);

        await _stravaTokenRepository.Received(1).UpsertAsync(
            Arg.Is<StravaConnection>(c =>
                c.AthleteProfileId == profile.Id &&
                c.AccessToken == tokenResponse.AccessToken &&
                c.RefreshToken == tokenResponse.RefreshToken &&
                c.StravaAthleteId == tokenResponse.StravaAthleteId &&
                c.ExpiresAt == tokenResponse.ExpiresAt),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TokenExchangeFails_ThrowsDomainException()
    {
        var profile = StravaDomainFakers.AthleteProfile();
        var command = new ConnectStravaCommand(profile.UserId, "bad-auth-code");

        _athleteProfileRepository
            .GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>())
            .Returns(profile);

        _stravaTokenService
            .ExchangeCodeAsync(command.AuthCode, Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Strava returned 401 Unauthorized."));

        Func<Task> act = () => _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*authorisation code*");
    }

    [Fact]
    public async Task Handle_ExistingConnection_UpsertsNewTokens()
    {
        // An athlete reconnects Strava — the handler must always call UpsertAsync
        // so the repository can overwrite the stale tokens rather than insert a duplicate.
        var profile = StravaDomainFakers.AthleteProfile();
        var newTokens = StravaDomainFakers.StravaTokenResponse();
        var command = new ConnectStravaCommand(profile.UserId, "refreshed-auth-code");

        _athleteProfileRepository
            .GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>())
            .Returns(profile);

        _stravaTokenService
            .ExchangeCodeAsync(command.AuthCode, Arg.Any<CancellationToken>())
            .Returns(newTokens);

        await _sut.Handle(command, CancellationToken.None);

        await _stravaTokenRepository.Received(1).UpsertAsync(
            Arg.Is<StravaConnection>(c =>
                c.AthleteProfileId == profile.Id &&
                c.AccessToken == newTokens.AccessToken &&
                c.RefreshToken == newTokens.RefreshToken),
            Arg.Any<CancellationToken>());
    }
}
