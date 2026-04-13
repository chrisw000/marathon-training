using MarathonTraining.Application.Activities;
using MarathonTraining.Application.Common.Exceptions;
using MarathonTraining.Application.Common.Interfaces;
using MarathonTraining.Application.Tests.Fakers;
using MarathonTraining.Domain.Enums;
using MarathonTraining.Domain.Interfaces;
using MarathonTraining.Domain.ValueObjects;
using NSubstitute;

namespace MarathonTraining.Application.Tests.Activities;

public sealed class GetActivitiesHandlerTests
{
    private readonly IAthleteProfileRepository _profileRepo = Substitute.For<IAthleteProfileRepository>();
    private readonly IActivityRepository _activityRepo = Substitute.For<IActivityRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    private readonly GetActivitiesQueryHandler _sut;

    public GetActivitiesHandlerTests()
    {
        _sut = new GetActivitiesQueryHandler(_profileRepo, _activityRepo, _currentUser);
    }

    private static GetActivitiesQuery DefaultQuery(
        ActivityType? type = null,
        DateOnly? from = null,
        DateOnly? to = null,
        int page = 1,
        int pageSize = 20) =>
        new(type, from, to, page, pageSize);

    [Fact]
    public async Task Handle_NoActivities_ReturnsEmptyPage()
    {
        var profile = AthleteProfileFaker.Default();
        _currentUser.UserId.Returns(profile.UserId);
        _profileRepo.GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>()).Returns(profile);
        _activityRepo.GetByAthleteAndDateRangeAsync(profile.Id, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<Domain.Aggregates.Activity>());

        var result = await _sut.Handle(DefaultQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ActivitiesPresent_ReturnsMappedItems()
    {
        var profile = AthleteProfileFaker.Default();
        _currentUser.UserId.Returns(profile.UserId);
        _profileRepo.GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>()).Returns(profile);

        var activities = new[]
        {
            ActivityFaker.WithTss(ActivityFaker.Run(profile.Id), 55m),
            ActivityFaker.Ride(profile.Id),
            ActivityFaker.Strength(profile.Id),
        };

        _activityRepo.GetByAthleteAndDateRangeAsync(profile.Id, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(activities.ToList());

        var result = await _sut.Handle(DefaultQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task Handle_TypeFilter_ReturnsOnlyMatchingType()
    {
        var profile = AthleteProfileFaker.Default();
        _currentUser.UserId.Returns(profile.UserId);
        _profileRepo.GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>()).Returns(profile);

        var activities = new[]
        {
            ActivityFaker.Run(profile.Id),
            ActivityFaker.Ride(profile.Id),
            ActivityFaker.Run(profile.Id),
        };

        _activityRepo.GetByAthleteAndDateRangeAsync(profile.Id, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(activities.ToList());

        var result = await _sut.Handle(DefaultQuery(type: ActivityType.Run), CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(a => a.ActivityType.Should().Be("Run"));
    }

    [Fact]
    public async Task Handle_Pagination_SlicesCorrectly()
    {
        var profile = AthleteProfileFaker.Default();
        _currentUser.UserId.Returns(profile.UserId);
        _profileRepo.GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>()).Returns(profile);

        // 5 activities, page size 2 → 3 pages
        var activities = Enumerable.Range(0, 5)
            .Select(_ => ActivityFaker.Run(profile.Id))
            .ToList();

        _activityRepo.GetByAthleteAndDateRangeAsync(profile.Id, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(activities);

        var page1 = await _sut.Handle(DefaultQuery(page: 1, pageSize: 2), CancellationToken.None);
        var page2 = await _sut.Handle(DefaultQuery(page: 2, pageSize: 2), CancellationToken.None);
        var page3 = await _sut.Handle(DefaultQuery(page: 3, pageSize: 2), CancellationToken.None);

        page1.Items.Should().HaveCount(2);
        page1.HasPreviousPage.Should().BeFalse();
        page1.HasNextPage.Should().BeTrue();
        page1.TotalPages.Should().Be(3);

        page2.Items.Should().HaveCount(2);
        page2.HasPreviousPage.Should().BeTrue();
        page2.HasNextPage.Should().BeTrue();

        page3.Items.Should().HaveCount(1);
        page3.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ActivityWithTss_MapsTssScore()
    {
        var profile = AthleteProfileFaker.Default();
        _currentUser.UserId.Returns(profile.UserId);
        _profileRepo.GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>()).Returns(profile);

        var activity = ActivityFaker.WithTss(ActivityFaker.Run(profile.Id), 75.5m);

        _activityRepo.GetByAthleteAndDateRangeAsync(profile.Id, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<Domain.Aggregates.Activity> { activity });

        var result = await _sut.Handle(DefaultQuery(), CancellationToken.None);

        result.Items.Single().TssScore.Should().BeApproximately(75.5m, 0.01m);
    }

    [Fact]
    public async Task Handle_ProfileNotFound_ThrowsNotFoundException()
    {
        _currentUser.UserId.Returns("no-user");
        _profileRepo.GetByUserIdAsync("no-user", Arg.Any<CancellationToken>())
            .Returns((Domain.Aggregates.AthleteProfile?)null);

        Func<Task> act = () => _sut.Handle(DefaultQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ResultsOrderedByStartedAtDescending()
    {
        var profile = AthleteProfileFaker.Default();
        _currentUser.UserId.Returns(profile.UserId);
        _profileRepo.GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>()).Returns(profile);

        var activities = new[]
        {
            new Domain.Aggregates.Activity(
                Guid.NewGuid(), Guid.NewGuid(), profile.Id, ActivityType.Run, "Old run",
                DateTimeOffset.UtcNow.AddDays(-10), 3600, null, null),
            new Domain.Aggregates.Activity(
                Guid.NewGuid(), Guid.NewGuid(), profile.Id, ActivityType.Run, "Recent run",
                DateTimeOffset.UtcNow.AddDays(-1), 3600, null, null),
        };

        _activityRepo.GetByAthleteAndDateRangeAsync(profile.Id, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(activities.ToList());

        var result = await _sut.Handle(DefaultQuery(), CancellationToken.None);

        result.Items[0].Name.Should().Be("Recent run");
        result.Items[1].Name.Should().Be("Old run");
    }
}
