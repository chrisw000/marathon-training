using FluentValidation.TestHelper;
using MarathonTraining.Application.Activities;
using MarathonTraining.Application.Common.Exceptions;
using MarathonTraining.Application.Common.Interfaces;
using MarathonTraining.Application.Tests.Fakers;
using MarathonTraining.Domain.Enums;
using MarathonTraining.Domain.Interfaces;
using MarathonTraining.Domain.Services;
using MarathonTraining.Domain.ValueObjects;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace MarathonTraining.Application.Tests.Activities;

public sealed class LogManualActivityHandlerTests
{
    private readonly IAthleteProfileRepository _profileRepo = Substitute.For<IAthleteProfileRepository>();
    private readonly ITrainingWeekRepository _weekRepo = Substitute.For<ITrainingWeekRepository>();
    private readonly ITssCalculationService _tssService = Substitute.For<ITssCalculationService>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    private readonly LogManualActivityCommandHandler _sut;

    public LogManualActivityHandlerTests()
    {
        _sut = new LogManualActivityCommandHandler(
            _profileRepo, _weekRepo, _tssService, _currentUser);
    }

    private LogManualActivityCommand ValidCommand(int rpe = 7, int durationMinutes = 45) =>
        new("Morning Weights", ActivityType.Strength, DateTimeOffset.UtcNow, durationMinutes, rpe, null);

    [Fact]
    public async Task Handle_ValidCommand_CreatesActivityAndReturnsId()
    {
        var profile = AthleteProfileFaker.Default();
        _currentUser.UserId.Returns(profile.UserId);
        _profileRepo.GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>()).Returns(profile);
        _weekRepo.GetByAthleteAndDateAsync(profile.Id, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Aggregates.TrainingWeek?)null);

        _tssService.Calculate(Arg.Any<TssCalculationInputs>()).Returns(TssScore.Create(40m));

        var result = await _sut.Handle(ValidCommand(), CancellationToken.None);

        result.ActivityId.Should().NotBeEmpty();
        result.TssScore.Should().BeApproximately(40m, 0.01m);

        await _weekRepo.Received(1).AddAsync(Arg.Any<Domain.Aggregates.TrainingWeek>(), Arg.Any<CancellationToken>());
        await _weekRepo.Received(1).UpdateAsync(Arg.Any<Domain.Aggregates.TrainingWeek>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingWeek_AddsToItWithoutCreatingNew()
    {
        var profile = AthleteProfileFaker.Default();
        _currentUser.UserId.Returns(profile.UserId);
        _profileRepo.GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>()).Returns(profile);

        var existingWeek = new Domain.Aggregates.TrainingWeek(Guid.NewGuid(), profile.Id, DateOnly.FromDateTime(DateTime.UtcNow));
        _weekRepo.GetByAthleteAndDateAsync(profile.Id, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(existingWeek);

        _tssService.Calculate(Arg.Any<TssCalculationInputs>()).Returns(TssScore.Create(35m));

        await _sut.Handle(ValidCommand(), CancellationToken.None);

        await _weekRepo.DidNotReceive().AddAsync(Arg.Any<Domain.Aggregates.TrainingWeek>(), Arg.Any<CancellationToken>());
        await _weekRepo.Received(1).UpdateAsync(existingWeek, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TssCalculationFails_ReturnsNullTss()
    {
        var profile = AthleteProfileFaker.WithoutPhysiology();
        _currentUser.UserId.Returns(profile.UserId);
        _profileRepo.GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>()).Returns(profile);
        _weekRepo.GetByAthleteAndDateAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Aggregates.TrainingWeek?)null);

        _tssService.Calculate(Arg.Any<TssCalculationInputs>())
            .Throws(new Domain.Exceptions.DomainException("Insufficient data."));

        var result = await _sut.Handle(ValidCommand(), CancellationToken.None);

        result.TssScore.Should().BeNull();
        result.ActivityId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ProfileNotFound_ThrowsNotFoundException()
    {
        _currentUser.UserId.Returns("ghost-user");
        _profileRepo.GetByUserIdAsync("ghost-user", Arg.Any<CancellationToken>())
            .Returns((Domain.Aggregates.AthleteProfile?)null);

        Func<Task> act = () => _sut.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

public sealed class LogManualActivityCommandValidatorTests
{
    private readonly LogManualActivityCommandValidator _validator = new();

    private static LogManualActivityCommand Valid() =>
        new("Morning Weights", ActivityType.Strength, DateTimeOffset.UtcNow, 45, 7, null);

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
    {
        var result = _validator.TestValidate(Valid());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(ActivityType.Run)]
    [InlineData(ActivityType.Ride)]
    public void Validate_NonStrengthActivity_FailsValidation(ActivityType type)
    {
        var cmd = Valid() with { ActivityType = type };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.ActivityType);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(480)]
    [InlineData(600)]
    public void Validate_InvalidDurationMinutes_FailsValidation(int durationMinutes)
    {
        var cmd = Valid() with { DurationMinutes = durationMinutes };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.DurationMinutes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(-1)]
    public void Validate_RpeOutOfRange_FailsValidation(int rpe)
    {
        var cmd = Valid() with { Rpe = rpe };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Rpe);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void Validate_RpeInRange_PassesValidation(int rpe)
    {
        var cmd = Valid() with { Rpe = rpe };
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(x => x.Rpe);
    }

    [Fact]
    public void Validate_EmptyName_FailsValidation()
    {
        var cmd = Valid() with { Name = "" };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(60)]
    [InlineData(479)]
    public void Validate_ValidDurationMinutes_PassesValidation(int durationMinutes)
    {
        var cmd = Valid() with { DurationMinutes = durationMinutes };
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(x => x.DurationMinutes);
    }
}
