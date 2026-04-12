using MarathonTraining.Application.Common.Exceptions;
using MarathonTraining.Application.Tests.Fakers;
using MarathonTraining.Application.Training;
using MarathonTraining.Domain.Exceptions;
using MarathonTraining.Domain.Interfaces;
using MarathonTraining.Domain.Services;
using MarathonTraining.Domain.ValueObjects;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace MarathonTraining.Application.Tests.Training;

public sealed class RecalculateTssHandlerTests
{
    private readonly IActivityRepository _activityRepo =
        Substitute.For<IActivityRepository>();

    private readonly IAthleteProfileRepository _profileRepo =
        Substitute.For<IAthleteProfileRepository>();

    private readonly ITssCalculationService _tssService =
        Substitute.For<ITssCalculationService>();

    private readonly RecalculateTssCommandHandler _sut;

    public RecalculateTssHandlerTests()
    {
        _sut = new RecalculateTssCommandHandler(_activityRepo, _profileRepo, _tssService);
    }

    [Fact]
    public async Task Handle_MultipleActivities_RecalculatesAllAndReturnsCount()
    {
        var profile = AthleteProfileFaker.Default();
        var activities = new[]
        {
            ActivityFaker.Run(athleteProfileId: profile.Id),
            ActivityFaker.Ride(athleteProfileId: profile.Id),
            ActivityFaker.Strength(athleteProfileId: profile.Id)
        };

        _profileRepo.GetByIdAsync(profile.Id, Arg.Any<CancellationToken>()).Returns(profile);
        _activityRepo.GetAllByAthleteIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<Domain.Aggregates.Activity>)activities);
        _tssService.Calculate(Arg.Any<TssCalculationInputs>()).Returns(TssScore.Create(50m));

        var count = await _sut.Handle(new RecalculateTssCommand(profile.Id), CancellationToken.None);

        count.Should().Be(3);
        await _activityRepo.Received(1).UpdateRangeAsync(
            Arg.Any<IEnumerable<Domain.Aggregates.Activity>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ActivityWithNullDataThrowsDomainException_IsSkippedGracefully()
    {
        var profile = AthleteProfileFaker.Default();
        // Strength activity with no RPE — StrengthTssCalculator will throw, handler skips it.
        // We use the real service here by making the mock throw for Strength but succeed for Run.
        var strengthNoRpe = ActivityFaker.Strength(athleteProfileId: profile.Id, rpe: null);
        var goodRun = ActivityFaker.Run(athleteProfileId: profile.Id);

        _profileRepo.GetByIdAsync(profile.Id, Arg.Any<CancellationToken>()).Returns(profile);
        _activityRepo.GetAllByAthleteIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<Domain.Aggregates.Activity>)new[] { strengthNoRpe, goodRun });

        _tssService.Calculate(Arg.Is<TssCalculationInputs>(i => i.ActivityType == Domain.Enums.ActivityType.Strength))
            .Throws(new DomainException("TSS calculation failed for Strength: RPE score is required."));
        _tssService.Calculate(Arg.Is<TssCalculationInputs>(i => i.ActivityType == Domain.Enums.ActivityType.Run))
            .Returns(TssScore.Create(60m));

        var count = await _sut.Handle(new RecalculateTssCommand(profile.Id), CancellationToken.None);

        count.Should().Be(1, "Strength activity with no RPE should be skipped");
    }

    [Fact]
    public async Task Handle_EmptyActivityList_ReturnsZero()
    {
        var profile = AthleteProfileFaker.Default();

        _profileRepo.GetByIdAsync(profile.Id, Arg.Any<CancellationToken>()).Returns(profile);
        _activityRepo.GetAllByAthleteIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<Domain.Aggregates.Activity>)Array.Empty<Domain.Aggregates.Activity>());

        var count = await _sut.Handle(new RecalculateTssCommand(profile.Id), CancellationToken.None);

        count.Should().Be(0);
        await _activityRepo.DidNotReceive().UpdateRangeAsync(
            Arg.Any<IEnumerable<Domain.Aggregates.Activity>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AthleteNotFound_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _profileRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Domain.Aggregates.AthleteProfile?)null);

        var act = async () => await _sut.Handle(new RecalculateTssCommand(id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
