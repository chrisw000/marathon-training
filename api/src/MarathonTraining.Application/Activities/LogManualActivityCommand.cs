using FluentValidation;
using MarathonTraining.Application.Common.Exceptions;
using MarathonTraining.Application.Common.Interfaces;
using MarathonTraining.Domain.Aggregates;
using MarathonTraining.Domain.Enums;
using MarathonTraining.Domain.Exceptions;
using MarathonTraining.Domain.Interfaces;
using MarathonTraining.Domain.Services;
using MarathonTraining.Domain.ValueObjects;
using MediatR;

namespace MarathonTraining.Application.Activities;

public record LogManualActivityCommand(
    string Name,
    ActivityType ActivityType,
    DateTimeOffset StartedAt,
    int DurationMinutes,
    int Rpe,
    string? Notes)
    : IRequest<LogManualActivityResult>;

public record LogManualActivityResult(Guid ActivityId, decimal? TssScore);

public sealed class LogManualActivityCommandValidator : AbstractValidator<LogManualActivityCommand>
{
    public LogManualActivityCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.");

        RuleFor(x => x.ActivityType)
            .Must(t => t == ActivityType.Strength)
            .WithMessage("Manual logging only supports Strength activities.");

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0).WithMessage("DurationMinutes must be greater than zero.")
            .LessThan(480).WithMessage("DurationMinutes must be less than 480.");

        RuleFor(x => x.Rpe)
            .InclusiveBetween(1, 10).WithMessage("Rpe must be between 1 and 10.");
    }
}

public sealed class LogManualActivityCommandHandler(
    IAthleteProfileRepository profileRepository,
    ITrainingWeekRepository weekRepository,
    ITssCalculationService tssService,
    ICurrentUserService currentUser)
    : IRequestHandler<LogManualActivityCommand, LogManualActivityResult>
{
    public async Task<LogManualActivityResult> Handle(
        LogManualActivityCommand request,
        CancellationToken cancellationToken)
    {
        var profile = await profileRepository.GetByUserIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException($"Athlete profile for user '{currentUser.UserId}' not found.");

        var durationSeconds = request.DurationMinutes * 60;
        var rpe = RpeScore.Create(request.Rpe);

        var weekStart = GetWeekStart(DateOnly.FromDateTime(request.StartedAt.Date));
        var week = await weekRepository.GetByAthleteAndDateAsync(profile.Id, weekStart, cancellationToken);

        if (week is null)
        {
            week = new TrainingWeek(Guid.NewGuid(), profile.Id, weekStart);
            await weekRepository.AddAsync(week, cancellationToken);
        }

        var activity = new Activity(
            id: Guid.NewGuid(),
            trainingWeekId: week.Id,
            athleteProfileId: profile.Id,
            activityType: ActivityType.Strength,
            name: request.Name,
            startedAt: request.StartedAt,
            durationSeconds: durationSeconds,
            distanceMetres: null,
            tssScore: null,
            rpeValue: request.Rpe);

        TssScore? tss = null;
        try
        {
            var duration = ActivityDuration.Create(durationSeconds);
            var inputs = new TssCalculationInputs(
                ActivityType: ActivityType.Strength,
                Duration: duration,
                HeartRate: null,
                AthleteHrZones: profile.HeartRateZones,
                NormalisedPower: null,
                Ftp: profile.Ftp,
                Rpe: rpe);

            tss = tssService.Calculate(inputs);
            activity.AssignTss(tss);
        }
        catch (DomainException)
        {
            // Insufficient data — TSS stays null; can be recalculated later.
        }

        week.AddActivity(activity);
        await weekRepository.UpdateAsync(week, cancellationToken);

        return new LogManualActivityResult(activity.Id, tss?.Value);
    }

    private static DateOnly GetWeekStart(DateOnly date)
    {
        var dow = (int)date.DayOfWeek;
        var daysToMonday = dow == 0 ? 6 : dow - 1;
        return date.AddDays(-daysToMonday);
    }
}
