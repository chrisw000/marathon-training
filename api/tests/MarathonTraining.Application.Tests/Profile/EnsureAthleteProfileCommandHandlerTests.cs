using Bogus;
using MarathonTraining.Application.Profile;
using MarathonTraining.Domain.Aggregates;
using MarathonTraining.Domain.Interfaces;
using NSubstitute;

namespace MarathonTraining.Application.Tests.Profile;

public sealed class EnsureAthleteProfileCommandHandlerTests
{
    private static readonly Faker Fake = new();

    private readonly IAthleteProfileRepository _profileRepository =
        Substitute.For<IAthleteProfileRepository>();

    private readonly EnsureAthleteProfileCommandHandler _handler;

    public EnsureAthleteProfileCommandHandlerTests()
    {
        _handler = new EnsureAthleteProfileCommandHandler(_profileRepository);
    }

    [Fact]
    public async Task Handle_NewUser_CreatesProfileAndReturnsWasCreatedTrue()
    {
        var userId = Guid.NewGuid().ToString();
        var displayName = Fake.Name.FullName();

        _profileRepository.GetByUserIdAsync(userId).Returns((AthleteProfile?)null);
        _profileRepository.AddAsync(Arg.Any<AthleteProfile>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _handler.Handle(
            new EnsureAthleteProfileCommand(userId, displayName),
            CancellationToken.None);

        result.WasCreated.Should().BeTrue();

        await _profileRepository.Received(1).AddAsync(
            Arg.Is<AthleteProfile>(p =>
                p.UserId == userId &&
                p.DisplayName == displayName),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ConcurrentInsert_ReturnsWasCreatedFalseWithoutThrowing()
    {
        // Simulates two simultaneous logins: both pass the GetByUserId check,
        // but the repository signals that a concurrent request won the race.
        var userId = Guid.NewGuid().ToString();
        var displayName = Fake.Name.FullName();

        _profileRepository.GetByUserIdAsync(userId).Returns((AthleteProfile?)null);
        _profileRepository.AddAsync(Arg.Any<AthleteProfile>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _handler.Handle(
            new EnsureAthleteProfileCommand(userId, displayName),
            CancellationToken.None);

        result.WasCreated.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ExistingUser_SkipsCreationAndReturnsWasCreatedFalse()
    {
        var existing = new AthleteProfile(
            Guid.NewGuid(),
            Guid.NewGuid().ToString(),
            Fake.Name.FullName(),
            DateTimeOffset.UtcNow.AddDays(-1));

        _profileRepository.GetByUserIdAsync(existing.UserId).Returns(existing);

        var result = await _handler.Handle(
            new EnsureAthleteProfileCommand(existing.UserId, existing.DisplayName),
            CancellationToken.None);

        result.WasCreated.Should().BeFalse();

        await _profileRepository.DidNotReceive().AddAsync(
            Arg.Any<AthleteProfile>(),
            Arg.Any<CancellationToken>());
    }
}
