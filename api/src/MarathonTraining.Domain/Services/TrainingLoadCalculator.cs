using MarathonTraining.Domain.ValueObjects;

namespace MarathonTraining.Domain.Services;

public static class TrainingLoadCalculator
{
    public static IReadOnlyList<TrainingLoad> Calculate(
        IReadOnlyList<(DateOnly Date, decimal DailyTss)> history)
    {
        if (history.Count == 0)
            return [];

        // Sort ascending and expand gaps (missing dates treated as 0 TSS).
        var sorted = history.OrderBy(h => h.Date).ToList();
        var expanded = ExpandGaps(sorted);

        var result = new List<TrainingLoad>(expanded.Count);
        double atl = 0.0;
        double ctl = 0.0;

        foreach (var (date, tss) in expanded)
        {
            double tsb = ctl - atl;            // TSB is yesterday's CTL − ATL
            double atlToday = atl + ((double)tss - atl) / 7.0;
            double ctlToday = ctl + ((double)tss - ctl) / 42.0;

            result.Add(TrainingLoad.Create(
                atl: Math.Round((decimal)atlToday, 4),
                ctl: Math.Round((decimal)ctlToday, 4),
                tsb: Math.Round((decimal)tsb, 4),
                date: date));

            atl = atlToday;
            ctl = ctlToday;
        }

        return result.AsReadOnly();
    }

    private static List<(DateOnly Date, decimal DailyTss)> ExpandGaps(
        List<(DateOnly Date, decimal DailyTss)> sorted)
    {
        var expanded = new List<(DateOnly, decimal)>();
        DateOnly? prev = null;

        foreach (var entry in sorted)
        {
            if (prev is not null)
            {
                var cursor = prev.Value.AddDays(1);
                while (cursor < entry.Date)
                {
                    expanded.Add((cursor, 0m));
                    cursor = cursor.AddDays(1);
                }
            }

            expanded.Add(entry);
            prev = entry.Date;
        }

        return expanded;
    }
}
