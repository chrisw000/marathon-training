using MarathonTraining.Application.Athlete;
using MarathonTraining.Application.Common.Exceptions;
using MarathonTraining.Application.Common.Interfaces;
using MarathonTraining.Application.Tests.Fakers;
using MarathonTraining.Domain.Interfaces;
using NSubstitute;

namespace MarathonTraining.Application.Tests.Athlete;

public sealed class GetAthleteProfileHandlerTests
{
    private readonly IAthleteProfileRepository _repository =
        Substitute.For<IAthleteProfileRepository>();

    private readonly ICurrentUserService _currentUser =
        Substitute.For<ICurrentUserService>();

    private readonly GetAthleteProfileQueryHandler _sut;

    public GetAthleteProfileHandlerTests()
    {
        _sut = new GetAthleteProfileQueryHandler(_repository, _currentUser);
    }

    [Fact]
    public async Task Handle_ProfileExists_ReturnsMappedResponse()
    {
        var profile = AthleteProfileFaker.Default();
        _currentUser.UserId.Returns(profile.UserId);
        _repository.GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>())
            .Returns(profile);

        var result = await _sut.Handle(new GetAthleteProfileQuery(), CancellationToken.None);

        result.Id.Should().Be(profile.Id);
        result.DisplayName.Should().Be(profile.DisplayName);
        result.CurrentPhase.Should().Be(profile.CurrentPhase.ToString());
        result.HasStravaConnected.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ProfileNotFound_ThrowsNotFoundException()
    {
        _currentUser.UserId.Returns("unknown-user");
        _repository.GetByUserIdAsync("unknown-user", Arg.Any<CancellationToken>())
            .Returns((Domain.Aggregates.AthleteProfile?)null);

        var act = async () => await _sut.Handle(new GetAthleteProfileQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ProfileWithNoPhysiology_MapsHrZonesAsNull()
    {
        var profile = AthleteProfileFaker.WithoutPhysiology();
        _currentUser.UserId.Returns(profile.UserId);
        _repository.GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>())
            .Returns(profile);

        var result = await _sut.Handle(new GetAthleteProfileQuery(), CancellationToken.None);

        result.RestingHr.Should().BeNull();
        result.MaxHr.Should().BeNull();
        result.ThresholdHr.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ProfileWithNoPhysiology_MapsFtpAsNull()
    {
        var profile = AthleteProfileFaker.WithoutPhysiology();
        _currentUser.UserId.Returns(profile.UserId);
        _repository.GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>())
            .Returns(profile);

        var result = await _sut.Handle(new GetAthleteProfileQuery(), CancellationToken.None);

        result.FtpWatts.Should().BeNull();
    }
}
