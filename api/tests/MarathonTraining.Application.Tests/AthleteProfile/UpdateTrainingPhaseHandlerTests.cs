using FluentValidation;
using MarathonTraining.Application.Athlete;
using MarathonTraining.Application.Common.Exceptions;
using MarathonTraining.Application.Common.Interfaces;
using MarathonTraining.Application.Tests.Fakers;
using MarathonTraining.Domain.Enums;
using MarathonTraining.Domain.Interfaces;
using NSubstitute;

namespace MarathonTraining.Application.Tests.Athlete;

public sealed class UpdateTrainingPhaseHandlerTests
{
    private readonly IAthleteProfileRepository _repository =
        Substitute.For<IAthleteProfileRepository>();

    private readonly ICurrentUserService _currentUser =
        Substitute.For<ICurrentUserService>();

    private readonly UpdateTrainingPhaseCommandHandler _sut;

    public UpdateTrainingPhaseHandlerTests()
    {
        _sut = new UpdateTrainingPhaseCommandHandler(_repository, _currentUser);
    }

    [Fact]
    public async Task Handle_ValidPhase_UpdatesPhaseAndReturnsResponse()
    {
        var profile = AthleteProfileFaker.Default();
        _currentUser.UserId.Returns(profile.UserId);
        _repository.GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>())
            .Returns(profile);

        var result = await _sut.Handle(new UpdateTrainingPhaseCommand("Build"), CancellationToken.None);

        result.CurrentPhase.Should().Be("Build");
        profile.CurrentPhase.Should().Be(TrainingPhase.Build);
    }

    [Fact]
    public async Task Handle_InvalidPhaseString_ThrowsValidationException()
    {
        var act = async () => await _sut.Handle(
            new UpdateTrainingPhaseCommand("NotAPhase"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Theory]
    [InlineData("Base")]
    [InlineData("Build")]
    [InlineData("RaceDevelopment")]
    [InlineData("Peak")]
    [InlineData("Taper")]
    public async Task Handle_AllValidPhaseNames_AreAccepted(string phaseName)
    {
        var profile = AthleteProfileFaker.Default();
        _currentUser.UserId.Returns(profile.UserId);
        _repository.GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>())
            .Returns(profile);

        var result = await _sut.Handle(new UpdateTrainingPhaseCommand(phaseName), CancellationToken.None);

        result.CurrentPhase.Should().Be(phaseName);
    }

    // ── Validator tests ────────────────────────────────────────────────────────

    [Fact]
    public void Validator_InvalidPhase_IsInvalid()
    {
        var validator = new UpdateTrainingPhaseCommandValidator();
        var result = validator.Validate(new UpdateTrainingPhaseCommand("InvalidPhase"));

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("Base")]
    [InlineData("Build")]
    [InlineData("RaceDevelopment")]
    [InlineData("Peak")]
    [InlineData("Taper")]
    public void Validator_AllValidPhaseNames_AreValid(string phaseName)
    {
        var validator = new UpdateTrainingPhaseCommandValidator();
        var result = validator.Validate(new UpdateTrainingPhaseCommand(phaseName));

        result.IsValid.Should().BeTrue($"'{phaseName}' is a valid TrainingPhase value");
    }
}
