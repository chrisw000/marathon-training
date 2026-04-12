using MarathonTraining.Application.Athlete;
using MarathonTraining.Application.Common.Exceptions;
using MarathonTraining.Application.Common.Interfaces;
using MarathonTraining.Application.Tests.Fakers;
using MarathonTraining.Domain.Interfaces;
using NSubstitute;

namespace MarathonTraining.Application.Tests.Athlete;

public sealed class UpdateAthletePhysiologyHandlerTests
{
    private readonly IAthleteProfileRepository _repository =
        Substitute.For<IAthleteProfileRepository>();

    private readonly ICurrentUserService _currentUser =
        Substitute.For<ICurrentUserService>();

    private readonly UpdateAthletePhysiologyCommandHandler _sut;

    public UpdateAthletePhysiologyHandlerTests()
    {
        _sut = new UpdateAthletePhysiologyCommandHandler(_repository, _currentUser);
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesPhysiologyAndReturnsResponse()
    {
        var profile = AthleteProfileFaker.WithoutPhysiology();
        _currentUser.UserId.Returns(profile.UserId);
        _repository.GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>())
            .Returns(profile);

        var command = new UpdateAthletePhysiologyCommand(
            RestingHr: 55,
            MaxHr: 185,
            ThresholdHr: 168,
            FtpWatts: 260);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.RestingHr.Should().Be(55);
        result.MaxHr.Should().Be(185);
        result.ThresholdHr.Should().Be(168);
        result.FtpWatts.Should().Be(260);
    }

    [Fact]
    public async Task Handle_ProfileNotFound_ThrowsNotFoundException()
    {
        _currentUser.UserId.Returns("ghost-user");
        _repository.GetByUserIdAsync("ghost-user", Arg.Any<CancellationToken>())
            .Returns((Domain.Aggregates.AthleteProfile?)null);

        var command = new UpdateAthletePhysiologyCommand(55, 185, 168, 260);
        var act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Success_CallsRepositoryUpdateAsyncExactlyOnce()
    {
        var profile = AthleteProfileFaker.WithoutPhysiology();
        _currentUser.UserId.Returns(profile.UserId);
        _repository.GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>())
            .Returns(profile);

        await _sut.Handle(new UpdateAthletePhysiologyCommand(55, 185, 168, 260), CancellationToken.None);

        await _repository.Received(1).UpdateAsync(
            Arg.Is<Domain.Aggregates.AthleteProfile>(p => p.UserId == profile.UserId),
            Arg.Any<CancellationToken>());
    }

    // ── Validator tests ────────────────────────────────────────────────────────

    [Fact]
    public void Validator_MaxHrAboveTwoTwenty_IsInvalid()
    {
        var validator = new UpdateAthletePhysiologyCommandValidator();
        var result = validator.Validate(new UpdateAthletePhysiologyCommand(55, 221, 168, 260));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateAthletePhysiologyCommand.MaxHr));
    }

    [Fact]
    public void Validator_ThresholdHrGreaterThanOrEqualToMaxHr_IsInvalid()
    {
        var validator = new UpdateAthletePhysiologyCommandValidator();
        var result = validator.Validate(new UpdateAthletePhysiologyCommand(55, 185, 185, 260));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateAthletePhysiologyCommand.ThresholdHr));
    }

    [Fact]
    public void Validator_FtpWattsAtOrBelowZero_IsInvalid()
    {
        var validator = new UpdateAthletePhysiologyCommandValidator();
        var result = validator.Validate(new UpdateAthletePhysiologyCommand(55, 185, 168, 0));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateAthletePhysiologyCommand.FtpWatts));
    }
}
