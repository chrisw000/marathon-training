using MarathonTraining.Application.Common.Exceptions;
using MarathonTraining.Application.Tests.Fakers;
using MarathonTraining.Application.Training;
using MarathonTraining.Domain.Enums;
using MarathonTraining.Domain.Exceptions;
using MarathonTraining.Domain.Interfaces;
using MarathonTraining.Domain.Services;
using MarathonTraining.Domain.ValueObjects;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace MarathonTraining.Application.Tests.Training;

public sealed class CalculateTssHandlerTests
{
    private readonly IActivityRepository _activityRepo =
        Substitute.For<IActivityRepository>();

    private readonly IAthleteProfileRepository _profileRepo =
        Substitute.For<IAthleteProfileRepository>();

    private readonly ITssCalculationService _tssService =
        Substitute.For<ITssCalculationService>();

    private readonly CalculateTssCommandHandler _sut;

    public CalculateTssHandlerTests()
    {
        _sut = new CalculateTssCommandHandler(_activityRepo, _profileRepo, _tssService);
    }

    [Fact]
    public async Task Handle_RunActivity_CalculatesAndStoresTss()
    {
        var profile = AthleteProfileFaker.Default();
        var activity = ActivityFaker.Run(athleteProfileId: profile.Id);
        var expectedTss = TssScore.Create(75m);

        _activityRepo.GetByIdAsync(activity.Id, Arg.Any<CancellationToken>()).Returns(activity);
        _profileRepo.GetByIdAsync(profile.Id, Arg.Any<CancellationToken>()).Returns(profile);
        _tssService.Calculate(Arg.Any<TssCalculationInputs>()).Returns(expectedTss);

        var result = await _sut.Handle(new CalculateTssCommand(activity.Id), CancellationToken.None);

        result.Should().Be(expectedTss);
        await _activityRepo.Received(1).UpdateAsync(
            Arg.Is<Domain.Aggregates.Activity>(a => a.TssScore == expectedTss),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RideActivity_CalculatesAndStoresTss()
    {
        var profile = AthleteProfileFaker.Default();
        var activity = ActivityFaker.Ride(athleteProfileId: profile.Id);
        var expectedTss = TssScore.Create(100m);

        _activityRepo.GetByIdAsync(activity.Id, Arg.Any<CancellationToken>()).Returns(activity);
        _profileRepo.GetByIdAsync(profile.Id, Arg.Any<CancellationToken>()).Returns(profile);
        _tssService.Calculate(Arg.Any<TssCalculationInputs>()).Returns(expectedTss);

        var result = await _sut.Handle(new CalculateTssCommand(activity.Id), CancellationToken.None);

        result.Should().Be(expectedTss);
    }

    [Fact]
    public async Task Handle_ActivityNotFound_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _activityRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Domain.Aggregates.Activity?)null);

        var act = async () => await _sut.Handle(new CalculateTssCommand(id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ProfileNotFound_ThrowsNotFoundException()
    {
        var activity = ActivityFaker.Run();
        _activityRepo.GetByIdAsync(activity.Id, Arg.Any<CancellationToken>()).Returns(activity);
        _profileRepo.GetByIdAsync(activity.AthleteProfileId, Arg.Any<CancellationToken>())
            .Returns((Domain.Aggregates.AthleteProfile?)null);

        var act = async () => await _sut.Handle(new CalculateTssCommand(activity.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_TssServiceThrowsDomainException_PropagatesCorrectly()
    {
        var profile = AthleteProfileFaker.Default();
        var activity = ActivityFaker.Run(athleteProfileId: profile.Id);

        _activityRepo.GetByIdAsync(activity.Id, Arg.Any<CancellationToken>()).Returns(activity);
        _profileRepo.GetByIdAsync(profile.Id, Arg.Any<CancellationToken>()).Returns(profile);
        _tssService.Calculate(Arg.Any<TssCalculationInputs>())
            .Throws(new DomainException("TSS calculation failed for Run: no HR data."));

        var act = async () => await _sut.Handle(new CalculateTssCommand(activity.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*TSS calculation failed*");
    }

    [Fact]
    public async Task Handle_Success_CallsUpdateAsyncExactlyOnce()
    {
        var profile = AthleteProfileFaker.Default();
        var activity = ActivityFaker.Run(athleteProfileId: profile.Id);

        _activityRepo.GetByIdAsync(activity.Id, Arg.Any<CancellationToken>()).Returns(activity);
        _profileRepo.GetByIdAsync(profile.Id, Arg.Any<CancellationToken>()).Returns(profile);
        _tssService.Calculate(Arg.Any<TssCalculationInputs>()).Returns(TssScore.Create(50m));

        await _sut.Handle(new CalculateTssCommand(activity.Id), CancellationToken.None);

        await _activityRepo.Received(1).UpdateAsync(
            Arg.Any<Domain.Aggregates.Activity>(),
            Arg.Any<CancellationToken>());
    }
}
