using MarathonTraining.Application.Common.Exceptions;
using MarathonTraining.Domain.Exceptions;
using MarathonTraining.Domain.Interfaces;
using MarathonTraining.Domain.Services;
using MarathonTraining.Domain.ValueObjects;
using MediatR;

namespace MarathonTraining.Application.Training;

public record CalculateTssCommand(Guid ActivityId) : IRequest<TssScore>;

public sealed class CalculateTssCommandHandler(
    IActivityRepository activityRepository,
    IAthleteProfileRepository profileRepository,
    ITssCalculationService tssCalculationService)
    : IRequestHandler<CalculateTssCommand, TssScore>
{
    public async Task<TssScore> Handle(CalculateTssCommand request, CancellationToken cancellationToken)
    {
        var activity = await activityRepository.GetByIdAsync(request.ActivityId, cancellationToken)
            ?? throw new NotFoundException($"Activity '{request.ActivityId}' not found.");

        var profile = await profileRepository.GetByIdAsync(activity.AthleteProfileId, cancellationToken)
            ?? throw new NotFoundException($"Athlete profile '{activity.AthleteProfileId}' not found.");

        var inputs = BuildInputs(activity, profile);
        var tss = tssCalculationService.Calculate(inputs);

        activity.AssignTss(tss);
        await activityRepository.UpdateAsync(activity, cancellationToken);

        return tss;
    }

    private static TssCalculationInputs BuildInputs(
        Domain.Aggregates.Activity activity,
        Domain.Aggregates.AthleteProfile profile)
    {
        var duration = ActivityDuration.Create(activity.DurationSeconds);

        HeartRateReading? hrReading = null;
        if (activity.AverageHeartRateBpm is > 0 && activity.MaxHeartRateBpm is > 0)
        {
            try { hrReading = HeartRateReading.Create(activity.AverageHeartRateBpm.Value, activity.MaxHeartRateBpm.Value); }
            catch (DomainException) { /* skip if invalid */ }
        }

        NormalisedPower? np = null;
        if (activity.NormalisedPowerWatts is > 0)
        {
            try { np = NormalisedPower.Create(activity.NormalisedPowerWatts.Value); }
            catch (DomainException) { /* skip if invalid */ }
        }

        RpeScore? rpe = null;
        if (activity.RpeValue is >= 1 and <= 10)
        {
            try { rpe = RpeScore.Create(activity.RpeValue.Value); }
            catch (DomainException) { /* skip if invalid */ }
        }

        return new TssCalculationInputs(
            ActivityType: activity.ActivityType,
            Duration: duration,
            HeartRate: hrReading,
            AthleteHrZones: profile.HeartRateZones,
            NormalisedPower: np,
            Ftp: profile.Ftp,
            Rpe: rpe);
    }
}
