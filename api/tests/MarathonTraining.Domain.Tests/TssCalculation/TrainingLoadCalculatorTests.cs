using MarathonTraining.Domain.Services;

namespace MarathonTraining.Domain.Tests.TssCalculation;

public sealed class TrainingLoadCalculatorTests
{
    [Fact]
    public void Calculate_EmptyHistory_ReturnsEmptyList()
    {
        var result = TrainingLoadCalculator.Calculate([]);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Calculate_SingleDayFrom0_CorrectAtlCtlTsb()
    {
        var history = new List<(DateOnly, decimal)>
        {
            (new DateOnly(2026, 1, 1), 100m)
        };

        var result = TrainingLoadCalculator.Calculate(history);

        result.Should().HaveCount(1);
        var day = result[0];
        // TSB = 0 (seeded from 0) — this is yesterday's CTL - ATL before this day
        ((double)day.TrainingStressBalance).Should().BeApproximately(0.0, 0.01);
        // ATL = 0 + (100 - 0) / 7 ≈ 14.2857
        ((double)day.AcuteTrainingLoad).Should().BeApproximately(14.29, 0.01);
        // CTL = 0 + (100 - 0) / 42 ≈ 2.381
        ((double)day.ChronicTrainingLoad).Should().BeApproximately(2.38, 0.01);
    }

    [Fact]
    public void Calculate_RestDayAfterBuild_CorrectDecay()
    {
        // Seed ATL=80, CTL=70 by building history, then one 0-TSS day
        // To get ATL≈80, CTL≈70 from zero, we need many days.
        // Simulate: seed by running enough days at constant TSS, then add a rest day.
        // ATL steady-state at tss: ATL = tss (after many days)
        // For ATL≈80: use 80 TSS/day for ~28 days
        // For CTL≈70: use 70 TSS/day for ~168 days but we want both ~80/70 at same time.
        // Simpler: use 80 TSS/day for 7 days (ATL≈68, higher after more days) — imprecise.
        // Better: build with a specific known seed.
        // Use the formula backwards: after N days at tss T, ATL = T*(1 - (6/7)^N).
        // For seed ATL=80 exactly: not easily reversible without many days.
        // Per spec: "Pre-populate the history list with enough days to produce the desired
        // ATL/CTL seed values before the test day."
        // Approximation: 100 days at 80 TSS → ATL≈80, then check decay.
        var history = new List<(DateOnly, decimal)>();
        var baseDate = new DateOnly(2026, 1, 1);
        for (int i = 0; i < 100; i++)
            history.Add((baseDate.AddDays(i), 80m));

        // ATL after 100 days at 80 TSS ≈ 80 (steady state).
        // CTL after 100 days at 80 TSS: CTL = 80*(1 - exp(-100/42)) ≈ 80 * (1 - 0.0927) ≈ 72.6

        // Add a rest day (0 TSS)
        history.Add((baseDate.AddDays(100), 0m));

        var result = TrainingLoadCalculator.Calculate(history);
        var restDay = result[^1];

        // ATL decays from ~80 by /7: new ATL = 80 + (0-80)/7 ≈ 68.57
        ((double)restDay.AcuteTrainingLoad).Should().BeApproximately(68.57, 0.5);
        // CTL after 100 days at 80 TSS ≈ 72.8; after rest day: 72.8 × (41/42) ≈ 71.08
        ((double)restDay.ChronicTrainingLoad).Should().BeApproximately(71.0, 0.5);
    }

    [Fact]
    public void Calculate_SeededAtHighAtl_OvertrainingWarningOnRestDay()
    {
        // Build to ATL≈100, CTL≈65, then add 0-TSS day → TSB goes to -(ATL-CTL)
        // 100 days at 100 TSS → ATL≈100, CTL≈100 (both converge to TSS)
        // We need ATL >> CTL, which means recent spike in TSS.
        // Strategy: 168 days at 65 TSS → CTL≈65, ATL≈65; then 7 days at 200 → ATL spikes
        var history = new List<(DateOnly, decimal)>();
        var baseDate = new DateOnly(2026, 1, 1);

        for (int i = 0; i < 168; i++)
            history.Add((baseDate.AddDays(i), 65m));
        for (int i = 168; i < 175; i++)
            history.Add((baseDate.AddDays(i), 200m));
        // Rest day
        history.Add((baseDate.AddDays(175), 0m));

        var result = TrainingLoadCalculator.Calculate(history);
        var restDay = result[^1];

        // After 7 days at 200 TSS, ATL spikes to ~154 while CTL only reaches ~87.
        // TSB ≈ -(154 - 87) ≈ -67, which is below -50 → IsOvertrainingDanger, not just Warning.
        restDay.IsOvertrainingDanger.Should().BeTrue(
            "TSB should be below -50 after a spike in ATL while CTL lags behind");
    }

    [Fact]
    public void Calculate_TsbBetween0And25_IsRaceReady()
    {
        // Build balanced load then taper: many days at moderate TSS, then rest.
        // After 84 days at 80: ATL≈80, CTL≈80. After a short taper ATL drops faster than CTL.
        // TSB on day 3 of rest ≈ +17 (in [0,25] race-ready zone).
        // TSB on day 5+ exceeds 25 → no longer race ready (too fresh).
        var history = new List<(DateOnly, decimal)>();
        var baseDate = new DateOnly(2026, 1, 1);
        for (int i = 0; i < 84; i++)   // 84 days at 80 → CTL≈80, ATL≈80
            history.Add((baseDate.AddDays(i), 80m));
        for (int i = 84; i < 88; i++)  // 4 rest days — TSB enters [0,25] by day 3 and stays there
            history.Add((baseDate.AddDays(i), 0m));

        var result = TrainingLoadCalculator.Calculate(history);
        var taperEnd = result[^1];

        taperEnd.IsRaceReady.Should().BeTrue(
            "after 4-day taper, TSB should be between 0 and 25");
    }

    [Fact]
    public void Calculate_TsbBelow50_IsOvertrainingDanger()
    {
        // Massive training spike to force TSB < -50
        var history = new List<(DateOnly, decimal)>();
        var baseDate = new DateOnly(2026, 1, 1);
        for (int i = 0; i < 168; i++)
            history.Add((baseDate.AddDays(i), 60m));
        for (int i = 168; i < 175; i++)
            history.Add((baseDate.AddDays(i), 400m));
        history.Add((baseDate.AddDays(175), 0m));

        var result = TrainingLoadCalculator.Calculate(history);
        var last = result[^1];

        last.IsOvertrainingDanger.Should().BeTrue("TSB should be below -50 after extreme spike");
    }

    [Fact]
    public void Calculate_HistoryWithGap_FillsGapWithZeroTssDays()
    {
        var d1 = new DateOnly(2026, 1, 1);
        var d2 = new DateOnly(2026, 1, 4);  // 2-day gap (Jan 2 and Jan 3 are missing)

        var history = new List<(DateOnly, decimal)>
        {
            (d1, 100m),
            (d2, 100m)
        };

        var result = TrainingLoadCalculator.Calculate(history);

        // Should have 4 entries: Jan 1, 2 (0 TSS), 3 (0 TSS), 4
        result.Should().HaveCount(4);
        result[0].Date.Should().Be(d1);
        result[3].Date.Should().Be(d2);
    }

    [Fact]
    public void Calculate_42DaysAt100Tss_CtlConvergesToApprox100()
    {
        var history = new List<(DateOnly, decimal)>();
        var baseDate = new DateOnly(2026, 1, 1);
        for (int i = 0; i < 168; i++)   // 4 × 42 days should be well converged
            history.Add((baseDate.AddDays(i), 100m));

        var result = TrainingLoadCalculator.Calculate(history);
        var last = result[^1];

        ((double)last.ChronicTrainingLoad).Should().BeApproximately(100.0, 2.0,
            "CTL should converge close to 100 after sustained 100 TSS/day");
    }
}
