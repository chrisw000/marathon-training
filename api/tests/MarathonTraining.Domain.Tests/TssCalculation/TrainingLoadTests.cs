using MarathonTraining.Domain.Exceptions;
using MarathonTraining.Domain.ValueObjects;

namespace MarathonTraining.Domain.Tests.TssCalculation;

public sealed class TrainingLoadTests
{
    private static readonly DateOnly AnyDate = new(2026, 1, 1);

    [Fact]
    public void Create_ValidValues_Succeeds()
    {
        var load = TrainingLoad.Create(50m, 60m, 10m, AnyDate);

        load.AcuteTrainingLoad.Should().Be(50m);
        load.ChronicTrainingLoad.Should().Be(60m);
        load.TrainingStressBalance.Should().Be(10m);
        load.Date.Should().Be(AnyDate);
    }

    [Fact]
    public void Create_NegativeAtl_ThrowsDomainException()
    {
        var act = () => TrainingLoad.Create(-1m, 60m, 10m, AnyDate);

        act.Should().Throw<DomainException>().WithMessage("*Acute training load*negative*");
    }

    [Fact]
    public void Create_NegativeCtl_ThrowsDomainException()
    {
        var act = () => TrainingLoad.Create(50m, -1m, 10m, AnyDate);

        act.Should().Throw<DomainException>().WithMessage("*Chronic training load*negative*");
    }

    [Theory]
    [InlineData(-31, true,  false, false, false, "Warning")]
    [InlineData(-29, false, false, false, true,  "Productive")]
    [InlineData(-51, false, true,  false, false, "Danger")]
    [InlineData(5,   false, false, true,  false, "Race ready")]
    [InlineData(-15, false, false, false, true,  "Productive")]
    [InlineData(30,  false, false, false, false, "Very fresh")]
    public void Create_TsbInRange_CorrectFlags(
        int tsb,
        bool expectWarning, bool expectDanger, bool expectRaceReady, bool expectProductive,
        string expectedDescription)
    {
        var load = TrainingLoad.Create(50m, 60m, tsb, AnyDate);

        load.IsOvertrainingWarning.Should().Be(expectWarning);
        load.IsOvertrainingDanger.Should().Be(expectDanger);
        load.IsRaceReady.Should().Be(expectRaceReady);
        load.IsProductive.Should().Be(expectProductive);
        load.FormDescription.Should().Be(expectedDescription);
    }

    [Fact]
    public void IsOvertrainingWarning_TsbMinus31_IsTrue()
    {
        var load = TrainingLoad.Create(0m, 0m, -31m, AnyDate);

        load.IsOvertrainingWarning.Should().BeTrue();
    }

    [Fact]
    public void IsOvertrainingWarning_TsbMinus29_IsFalse()
    {
        var load = TrainingLoad.Create(0m, 0m, -29m, AnyDate);

        load.IsOvertrainingWarning.Should().BeFalse();
    }

    [Fact]
    public void IsOvertrainingDanger_TsbMinus51_IsTrue()
    {
        var load = TrainingLoad.Create(0m, 0m, -51m, AnyDate);

        load.IsOvertrainingDanger.Should().BeTrue();
    }

    [Fact]
    public void IsRaceReady_TsbFive_IsTrue_IsProductive_IsFalse()
    {
        var load = TrainingLoad.Create(0m, 0m, 5m, AnyDate);

        load.IsRaceReady.Should().BeTrue();
        load.IsProductive.Should().BeFalse();
    }

    [Fact]
    public void IsProductive_TsbMinus15_IsTrue_IsRaceReady_IsFalse()
    {
        var load = TrainingLoad.Create(0m, 0m, -15m, AnyDate);

        load.IsProductive.Should().BeTrue();
        load.IsRaceReady.Should().BeFalse();
    }
}
