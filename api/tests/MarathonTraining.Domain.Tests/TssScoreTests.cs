using MarathonTraining.Domain.Exceptions;
using MarathonTraining.Domain.ValueObjects;

namespace MarathonTraining.Domain.Tests;

public sealed class TssScoreTests
{
    [Fact]
    public void Create_NegativeValue_ThrowsDomainException()
    {
        var act = () => TssScore.Create(-1m);

        act.Should().Throw<DomainException>()
            .WithMessage("*negative*");
    }

    [Fact]
    public void Create_ZeroValue_ReturnsScoreWithValueZero()
    {
        var score = TssScore.Create(0m);

        score.Value.Should().Be(0m);
    }

    [Fact]
    public void Create_PositiveValue_ReturnsScoreWithCorrectValue()
    {
        var score = TssScore.Create(85.5m);

        score.Value.Should().Be(85.5m);
    }
}
