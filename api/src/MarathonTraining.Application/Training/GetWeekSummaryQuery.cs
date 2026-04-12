using MarathonTraining.Application.Common.Exceptions;
using MarathonTraining.Domain.Enums;
using MarathonTraining.Domain.Interfaces;
using MarathonTraining.Domain.Services;
using MediatR;

namespace MarathonTraining.Application.Training;

public record GetWeekSummaryQuery(Guid AthleteProfileId, DateOnly WeekStartDate)
    : IRequest<WeekSummaryResponse>;

public record WeekSummaryResponse(
    DateOnly WeekStart,
    decimal TotalTss,
    int RunCount,
    int RideCount,
    int StrengthCount,
    decimal RunTss,
    decimal RideTss,
    decimal StrengthTss,
    TrainingLoadResponse TrainingLoad,
    bool HasOvertrainingWarning,
    string Recommendation);

public sealed class GetWeekSummaryQueryHandler(
    ITrainingWeekRepository weekRepository,
    IActivityRepository activityRepository,
    IAthleteProfileRepository profileRepository)
    : IRequestHandler<GetWeekSummaryQuery, WeekSummaryResponse>
{
    // Phase-appropriate weekly TSS target ranges (min, max)
    private static readonly Dictionary<TrainingPhase, (decimal Min, decimal Max)> PhaseTssTargets = new()
    {
        [TrainingPhase.Base]            = (300m, 450m),
        [TrainingPhase.Build]           = (450m, 600m),
        [TrainingPhase.RaceDevelopment] = (500m, 650m),
        [TrainingPhase.Peak]            = (500m, 650m),
        [TrainingPhase.Taper]           = (150m, 300m),
    };

    public async Task<WeekSummaryResponse> Handle(
        GetWeekSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var week = await weekRepository.GetByAthleteAndDateAsync(
            request.AthleteProfileId, request.WeekStartDate, cancellationToken)
            ?? throw new NotFoundException(
                $"Training week starting {request.WeekStartDate} not found for athlete '{request.AthleteProfileId}'.");

        var activities = week.Activities.Count > 0
            ? week.Activities
            : (IReadOnlyCollection<Domain.Aggregates.Activity>)
              await activityRepository.GetByTrainingWeekIdAsync(week.Id, cancellationToken);

        var runActivities      = activities.Where(a => a.ActivityType == ActivityType.Run).ToList();
        var rideActivities     = activities.Where(a => a.ActivityType == ActivityType.Ride).ToList();
        var strengthActivities = activities.Where(a => a.ActivityType == ActivityType.Strength).ToList();

        decimal runTss      = runActivities.Sum(a => a.TssScore?.Value ?? 0m);
        decimal rideTss     = rideActivities.Sum(a => a.TssScore?.Value ?? 0m);
        decimal strengthTss = strengthActivities.Sum(a => a.TssScore?.Value ?? 0m);
        decimal totalTss    = runTss + rideTss + strengthTss;

        // Get training load at the end of the week (Sunday)
        var weekEnd = request.WeekStartDate.AddDays(6);
        var seedFrom = request.WeekStartDate.AddDays(-42);

        var allActivities = await activityRepository.GetByAthleteAndDateRangeAsync(
            request.AthleteProfileId, seedFrom, weekEnd, cancellationToken);

        var dailyTss = allActivities
            .Where(a => a.TssScore is not null)
            .GroupBy(a => DateOnly.FromDateTime(a.StartedAt.Date))
            .Select(g => (Date: g.Key, DailyTss: g.Sum(a => a.TssScore!.Value)))
            .OrderBy(x => x.Date)
            .ToList();

        var allLoad = TrainingLoadCalculator.Calculate(dailyTss);
        var endLoad = allLoad.FirstOrDefault(l => l.Date == weekEnd)
                      ?? allLoad.LastOrDefault();

        TrainingLoadResponse loadResponse;
        if (endLoad is not null)
        {
            loadResponse = new TrainingLoadResponse(
                Date: endLoad.Date,
                Atl: endLoad.AcuteTrainingLoad,
                Ctl: endLoad.ChronicTrainingLoad,
                Tsb: endLoad.TrainingStressBalance,
                IsOvertrainingWarning: endLoad.IsOvertrainingWarning,
                IsOvertrainingDanger: endLoad.IsOvertrainingDanger,
                IsRaceReady: endLoad.IsRaceReady,
                FormDescription: endLoad.FormDescription,
                DailyTss: 0m);
        }
        else
        {
            loadResponse = new TrainingLoadResponse(
                Date: weekEnd,
                Atl: 0m, Ctl: 0m, Tsb: 0m,
                IsOvertrainingWarning: false, IsOvertrainingDanger: false, IsRaceReady: false,
                FormDescription: "Very fresh",
                DailyTss: 0m);
        }

        // Recommendation based on phase-appropriate target
        var profile = await profileRepository.GetByIdAsync(request.AthleteProfileId, cancellationToken);
        var phase = profile?.CurrentPhase ?? TrainingPhase.Base;
        var (minTarget, maxTarget) = PhaseTssTargets.GetValueOrDefault(phase, (300m, 450m));

        var recommendation = totalTss > maxTarget ? "Reduce load this week"
            : totalTss < minTarget                ? "Increase load this week"
            : "On track";

        return new WeekSummaryResponse(
            WeekStart: request.WeekStartDate,
            TotalTss: totalTss,
            RunCount: runActivities.Count,
            RideCount: rideActivities.Count,
            StrengthCount: strengthActivities.Count,
            RunTss: runTss,
            RideTss: rideTss,
            StrengthTss: strengthTss,
            TrainingLoad: loadResponse,
            HasOvertrainingWarning: endLoad?.IsOvertrainingWarning ?? false,
            Recommendation: recommendation);
    }
}
