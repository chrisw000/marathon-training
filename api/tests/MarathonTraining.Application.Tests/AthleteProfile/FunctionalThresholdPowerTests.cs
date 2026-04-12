using MarathonTraining.Domain.Exceptions;
using MarathonTraining.Domain.ValueObjects;

namespace MarathonTraining.Application.Tests.Athlete;

public sealed class FunctionalThresholdPowerTests
{
    [Fact]
    public void Create_ValidWatts_Succeeds()
    {
        var ftp = FunctionalThresholdPower.Create(250);

        ftp.Watts.Should().Be(250);
    }

    [Fact]
    public void Create_ZeroWatts_ThrowsDomainException()
    {
        var act = () => FunctionalThresholdPower.Create(0);

        act.Should().Throw<DomainException>()
            .WithMessage("*greater than zero*");
    }

    [Fact]
    public void Create_SixHundredWatts_ThrowsDomainException()
    {
        var act = () => FunctionalThresholdPower.Create(600);

        act.Should().Throw<DomainException>()
            .WithMessage("*less than 600*");
    }

    [Fact]
    public void Create_OneWatt_Succeeds()
    {
        var ftp = FunctionalThresholdPower.Create(1);

        ftp.Watts.Should().Be(1);
    }

    [Fact]
    public void Create_FiveNineNineWatts_Succeeds()
    {
        var ftp = FunctionalThresholdPower.Create(599);

        ftp.Watts.Should().Be(599);
    }
}
