using MarathonTraining.Application.Common.Exceptions;
using MarathonTraining.Domain.Exceptions;
using MarathonTraining.Domain.Interfaces;
using MarathonTraining.Domain.Services;
using MarathonTraining.Domain.ValueObjects;
using MediatR;

namespace MarathonTraining.Application.Training;

public record RecalculateTssCommand(Guid AthleteProfileId) : IRequest<int>;

public sealed class RecalculateTssCommandHandler(
    IActivityRepository activityRepository,
    IAthleteProfileRepository profileRepository,
    ITssCalculationService tssCalculationService)
    : IRequestHandler<RecalculateTssCommand, int>
{
    public async Task<int> Handle(RecalculateTssCommand request, CancellationToken cancellationToken)
    {
        var profile = await profileRepository.GetByIdAsync(request.AthleteProfileId, cancellationToken)
            ?? throw new NotFoundException($"Athlete profile '{request.AthleteProfileId}' not found.");

        var activities = await activityRepository.GetAllByAthleteIdAsync(request.AthleteProfileId, cancellationToken);

        if (activities.Count == 0)
            return 0;

        var updated = new List<Domain.Aggregates.Activity>();

        foreach (var activity in activities)
        {
            try
            {
                var duration = ActivityDuration.Create(activity.DurationSeconds);

                HeartRateReading? hrReading = null;
                if (activity.AverageHeartRateBpm is > 0 && activity.MaxHeartRateBpm is > 0)
                {
                    try { hrReading = HeartRateReading.Create(activity.AverageHeartRateBpm.Value, activity.MaxHeartRateBpm.Value); }
                    catch (DomainException) { }
                }

                NormalisedPower? np = null;
                if (activity.NormalisedPowerWatts is > 0)
                {
                    try { np = NormalisedPower.Create(activity.NormalisedPowerWatts.Value); }
                    catch (DomainException) { }
                }

                RpeScore? rpe = null;
                if (activity.RpeValue is >= 1 and <= 10)
                {
                    try { rpe = RpeScore.Create(activity.RpeValue.Value); }
                    catch (DomainException) { }
                }

                var inputs = new TssCalculationInputs(
                    ActivityType: activity.ActivityType,
                    Duration: duration,
                    HeartRate: hrReading,
                    AthleteHrZones: profile.HeartRateZones,
                    NormalisedPower: np,
                    Ftp: profile.Ftp,
                    Rpe: rpe);

                var tss = tssCalculationService.Calculate(inputs);
                activity.AssignTss(tss);
                updated.Add(activity);
            }
            catch (DomainException)
            {
                // Activity lacks sufficient data — skip gracefully.
            }
        }

        if (updated.Count > 0)
            await activityRepository.UpdateRangeAsync(updated, cancellationToken);

        return updated.Count;
    }
}
