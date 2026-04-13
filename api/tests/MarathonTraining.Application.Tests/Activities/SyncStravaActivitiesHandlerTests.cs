using MarathonTraining.Application.Activities;
using MarathonTraining.Application.Common.Exceptions;
using MarathonTraining.Application.Common.Interfaces;
using MarathonTraining.Application.Strava;
using MarathonTraining.Application.Tests.Fakers;
using MarathonTraining.Domain.Aggregates;
using MarathonTraining.Domain.Interfaces;
using MarathonTraining.Domain.Services;
using NSubstitute;

namespace MarathonTraining.Application.Tests.Activities;

public sealed class SyncStravaActivitiesHandlerTests
{
    private readonly IAthleteProfileRepository _profileRepo = Substitute.For<IAthleteProfileRepository>();
    private readonly IStravaTokenRepository _tokenRepo = Substitute.For<IStravaTokenRepository>();
    private readonly IStravaTokenService _tokenService = Substitute.For<IStravaTokenService>();
    private readonly IStravaActivityClient _stravaClient = Substitute.For<IStravaActivityClient>();
    private readonly IActivityRepository _activityRepo = Substitute.For<IActivityRepository>();
    private readonly ITrainingWeekRepository _weekRepo = Substitute.For<ITrainingWeekRepository>();
    private readonly ITssCalculationService _tssService = Substitute.For<ITssCalculationService>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    private readonly SyncStravaActivitiesCommandHandler _sut;

    public SyncStravaActivitiesHandlerTests()
    {
        _sut = new SyncStravaActivitiesCommandHandler(
            _profileRepo, _tokenRepo, _tokenService, _stravaClient,
            _activityRepo, _weekRepo, _tssService, _currentUser);
    }

    private void SetupProfile(out Domain.Aggregates.AthleteProfile profile, out Domain.Aggregates.StravaConnection connection)
    {
        profile = AthleteProfileFaker.Default();
        var capturedProfile = profile;
        connection = StravaDomainFakers.StravaConnection(
            athleteProfileId: profile.Id,
            expiresAt: DateTimeOffset.UtcNow.AddHours(2));

        _currentUser.UserId.Returns(profile.UserId);
        _profileRepo.GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>()).Returns(profile);
        _tokenRepo.GetByAthleteIdAsync(profile.Id, Arg.Any<CancellationToken>()).Returns(connection);

        // No existing activities by default
        _activityRepo.GetByStravaIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Aggregates.Activity?)null);

        // No existing week by default — handler creates a new one
        _weekRepo.GetByAthleteAndDateAsync(profile.Id, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns((TrainingWeek?)null);
    }

    [Fact]
    public async Task Handle_NoActivitiesFromStrava_ReturnsSyncedZero()
    {
        SetupProfile(out _, out _);

        _stravaClient
            .GetActivitiesAsync(Arg.Any<string>(), Arg.Any<long?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<StravaActivitySummary>());

        var result = await _sut.Handle(new SyncStravaActivitiesCommand(), CancellationToken.None);

        result.ActivitiesSynced.Should().Be(0);
        result.ActivitiesSkipped.Should().Be(0);
    }

    [Fact]
    public async Task Handle_NewActivity_SyncsItAndRecordsTimestamp()
    {
        SetupProfile(out var profile, out var connection);

        var summary = StravaActivitySummaryFaker.Run();

        _stravaClient
            .GetActivitiesAsync(connection.AccessToken, null, 1, 100, Arg.Any<CancellationToken>())
            .Returns(new List<StravaActivitySummary> { summary });

        _stravaClient
            .GetActivitiesAsync(Arg.Any<string>(), Arg.Any<long?>(), 2, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<StravaActivitySummary>());

        var result = await _sut.Handle(new SyncStravaActivitiesCommand(), CancellationToken.None);

        result.ActivitiesSynced.Should().Be(1);
        result.ActivitiesSkipped.Should().Be(0);

        await _weekRepo.Received(1).AddAsync(Arg.Any<TrainingWeek>(), Arg.Any<CancellationToken>());
        await _weekRepo.Received(1).UpdateAsync(Arg.Any<TrainingWeek>(), Arg.Any<CancellationToken>());
        await _profileRepo.Received(1).UpdateAsync(profile, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyExistingActivity_SkipsIt()
    {
        SetupProfile(out _, out var connection);

        var summary = StravaActivitySummaryFaker.Run();

        _stravaClient
            .GetActivitiesAsync(Arg.Any<string>(), Arg.Any<long?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(
                new List<StravaActivitySummary> { summary },
                new List<StravaActivitySummary>());

        // Existing activity found
        _activityRepo.GetByStravaIdAsync(summary.StravaId, Arg.Any<CancellationToken>())
            .Returns(ActivityFaker.Run());

        var result = await _sut.Handle(new SyncStravaActivitiesCommand(), CancellationToken.None);

        result.ActivitiesSynced.Should().Be(0);
        result.ActivitiesSkipped.Should().Be(1);
        await _weekRepo.DidNotReceive().AddAsync(Arg.Any<TrainingWeek>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingWeek_AddsToItWithoutCreatingNew()
    {
        SetupProfile(out var profile, out _);

        var summary = StravaActivitySummaryFaker.Run();
        var existingWeek = new TrainingWeek(Guid.NewGuid(), profile.Id, DateOnly.FromDateTime(DateTime.UtcNow));

        _stravaClient
            .GetActivitiesAsync(Arg.Any<string>(), Arg.Any<long?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(
                new List<StravaActivitySummary> { summary },
                new List<StravaActivitySummary>());

        _weekRepo.GetByAthleteAndDateAsync(profile.Id, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(existingWeek);

        var result = await _sut.Handle(new SyncStravaActivitiesCommand(), CancellationToken.None);

        result.ActivitiesSynced.Should().Be(1);
        await _weekRepo.DidNotReceive().AddAsync(Arg.Any<TrainingWeek>(), Arg.Any<CancellationToken>());
        await _weekRepo.Received(1).UpdateAsync(existingWeek, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TokenExpiringSoon_RefreshesBeforeSync()
    {
        var profile = AthleteProfileFaker.Default();

        // Token expiring within 5 minutes
        var expiringConnection = StravaDomainFakers.StravaConnection(
            athleteProfileId: profile.Id,
            expiresAt: DateTimeOffset.UtcNow.AddMinutes(3));

        _currentUser.UserId.Returns(profile.UserId);
        _profileRepo.GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>()).Returns(profile);
        _tokenRepo.GetByAthleteIdAsync(profile.Id, Arg.Any<CancellationToken>()).Returns(expiringConnection);

        _activityRepo.GetByStravaIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Aggregates.Activity?)null);
        _weekRepo.GetByAthleteAndDateAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns((TrainingWeek?)null);

        // Capture original token BEFORE handler mutates connection.RefreshToken via Update()
        var originalRefreshToken = expiringConnection.RefreshToken;
        var originalStravaAthleteId = expiringConnection.StravaAthleteId;

        var refreshed = StravaDomainFakers.StravaTokenResponse();
        _tokenService.RefreshTokenAsync(
            Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(refreshed);

        _stravaClient
            .GetActivitiesAsync(Arg.Any<string>(), Arg.Any<long?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<StravaActivitySummary>());

        await _sut.Handle(new SyncStravaActivitiesCommand(), CancellationToken.None);

        await _tokenService.Received(1).RefreshTokenAsync(
            originalRefreshToken,
            originalStravaAthleteId,
            Arg.Any<CancellationToken>());

        await _tokenRepo.Received(1).UpsertAsync(expiringConnection, Arg.Any<CancellationToken>());

        // Verify the refreshed token is used for the API call
        await _stravaClient.Received(1).GetActivitiesAsync(
            refreshed.AccessToken,
            Arg.Any<long?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoStravaConnection_ThrowsStravaNotConnectedException()
    {
        var profile = AthleteProfileFaker.Default();
        _currentUser.UserId.Returns(profile.UserId);
        _profileRepo.GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>()).Returns(profile);
        _tokenRepo.GetByAthleteIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns((Domain.Aggregates.StravaConnection?)null);

        Func<Task> act = () => _sut.Handle(new SyncStravaActivitiesCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<StravaNotConnectedException>();
    }

    [Fact]
    public async Task Handle_NoAthleteProfile_ThrowsNotFoundException()
    {
        _currentUser.UserId.Returns("unknown-user");
        _profileRepo.GetByUserIdAsync("unknown-user", Arg.Any<CancellationToken>())
            .Returns((Domain.Aggregates.AthleteProfile?)null);

        Func<Task> act = () => _sut.Handle(new SyncStravaActivitiesCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_IncrementalSync_BreaksAfterFirstPage()
    {
        // Profile with LastSyncedAt set — triggers incremental (after param) mode
        SetupProfile(out var profile, out _);
        profile.RecordSync(); // sets LastSyncedAt

        _profileRepo.GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>()).Returns(profile);

        var summary = StravaActivitySummaryFaker.Run();

        _stravaClient
            .GetActivitiesAsync(Arg.Any<string>(), Arg.Any<long?>(), 1, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<StravaActivitySummary> { summary });

        var result = await _sut.Handle(new SyncStravaActivitiesCommand(), CancellationToken.None);

        result.ActivitiesSynced.Should().Be(1);

        // Must NOT have requested page 2 — incremental syncs are single-call
        await _stravaClient.DidNotReceive().GetActivitiesAsync(
            Arg.Any<string>(), Arg.Any<long?>(), 2, Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
}
