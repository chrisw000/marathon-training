using MarathonTraining.Domain.Interfaces;
using MarathonTraining.Domain.Services;
using MediatR;

namespace MarathonTraining.Application.Training;

public record GetTrainingLoadQuery(Guid AthleteProfileId, DateOnly From, DateOnly To)
    : IRequest<IReadOnlyList<TrainingLoadResponse>>;

public record TrainingLoadResponse(
    DateOnly Date,
    decimal Atl,
    decimal Ctl,
    decimal Tsb,
    bool IsOvertrainingWarning,
    bool IsOvertrainingDanger,
    bool IsRaceReady,
    string FormDescription,
    decimal DailyTss);

public sealed class GetTrainingLoadQueryHandler(
    IActivityRepository activityRepository)
    : IRequestHandler<GetTrainingLoadQuery, IReadOnlyList<TrainingLoadResponse>>
{
    public async Task<IReadOnlyList<TrainingLoadResponse>> Handle(
        GetTrainingLoadQuery request,
        CancellationToken cancellationToken)
    {
        // Fetch from 42 days before From to seed CTL correctly.
        var seedFrom = request.From.AddDays(-42);

        var activities = await activityRepository.GetByAthleteAndDateRangeAsync(
            request.AthleteProfileId, seedFrom, request.To, cancellationToken);

        // Group by date and sum TSS per day.
        var dailyTss = activities
            .Where(a => a.TssScore is not null)
            .GroupBy(a => DateOnly.FromDateTime(a.StartedAt.Date))
            .Select(g => (Date: g.Key, DailyTss: g.Sum(a => a.TssScore!.Value)))
            .OrderBy(x => x.Date)
            .ToList();

        var allLoad = TrainingLoadCalculator.Calculate(dailyTss);

        // Return only the requested date range.
        return allLoad
            .Where(l => l.Date >= request.From && l.Date <= request.To)
            .Select(l =>
            {
                var tssOnDate = dailyTss.FirstOrDefault(d => d.Date == l.Date).DailyTss;
                return new TrainingLoadResponse(
                    Date: l.Date,
                    Atl: l.AcuteTrainingLoad,
                    Ctl: l.ChronicTrainingLoad,
                    Tsb: l.TrainingStressBalance,
                    IsOvertrainingWarning: l.IsOvertrainingWarning,
                    IsOvertrainingDanger: l.IsOvertrainingDanger,
                    IsRaceReady: l.IsRaceReady,
                    FormDescription: l.FormDescription,
                    DailyTss: tssOnDate);
            })
            .ToList()
            .AsReadOnly();
    }
}
