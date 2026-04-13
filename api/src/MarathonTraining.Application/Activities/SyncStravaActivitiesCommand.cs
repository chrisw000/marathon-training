using MarathonTraining.Application.Common.Exceptions;
using MarathonTraining.Application.Common.Interfaces;
using MarathonTraining.Application.Strava;
using MarathonTraining.Domain.Aggregates;
using MarathonTraining.Domain.Exceptions;
using MarathonTraining.Domain.Interfaces;
using MarathonTraining.Domain.Services;
using MarathonTraining.Domain.ValueObjects;
using MediatR;

namespace MarathonTraining.Application.Activities;

public record SyncStravaActivitiesCommand : IRequest<SyncResult>;

public record SyncResult(int ActivitiesSynced, int ActivitiesSkipped, DateTimeOffset SyncedAt);

public sealed class SyncStravaActivitiesCommandHandler(
    IAthleteProfileRepository profileRepository,
    IStravaTokenRepository tokenRepository,
    IStravaTokenService tokenService,
    IStravaActivityClient stravaClient,
    IActivityRepository activityRepository,
    ITrainingWeekRepository weekRepository,
    ITssCalculationService tssService,
    ICurrentUserService currentUser)
    : IRequestHandler<SyncStravaActivitiesCommand, SyncResult>
{
    public async Task<SyncResult> Handle(
        SyncStravaActivitiesCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Load athlete profile
        var profile = await profileRepository.GetByUserIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException($"Athlete profile for user '{currentUser.UserId}' not found.");

        // 2. Load Strava connection
        var connection = await tokenRepository.GetByAthleteIdAsync(profile.Id, cancellationToken)
            ?? throw new StravaNotConnectedException();

        // 3. Refresh token if expiring within 5 minutes
        var accessToken = connection.AccessToken;
        if (connection.ExpiresAt <= DateTimeOffset.UtcNow.AddMinutes(5))
        {
            var refreshed = await tokenService.RefreshTokenAsync(
                connection.RefreshToken,
                connection.StravaAthleteId,
                cancellationToken);

            connection.Update(refreshed.AccessToken, refreshed.RefreshToken, refreshed.ExpiresAt);
            await tokenRepository.UpsertAsync(connection, cancellationToken);
            accessToken = refreshed.AccessToken;
        }

        // 4. Determine sync start (null = full initial sync)
        long? afterEpoch = profile.LastSyncedAt?.ToUnixTimeSeconds();

        // 5. Paginate through activities
        var synced = 0;
        var skipped = 0;
        var page = 1;

        while (true)
        {
            var activities = await stravaClient.GetActivitiesAsync(
                accessToken, afterEpoch, page, 100, cancellationToken);

            if (activities.Count == 0)
                break;

            // 6. Process each activity
            foreach (var dto in activities)
            {
                var existing = await activityRepository.GetByStravaIdAsync(dto.StravaId, cancellationToken);
                if (existing is not null)
                {
                    skipped++;
                    continue;
                }

                var weekStart = GetWeekStart(DateOnly.FromDateTime(dto.StartedAt.Date));
                var week = await weekRepository.GetByAthleteAndDateAsync(
                    profile.Id, weekStart, cancellationToken);

                if (week is null)
                {
                    week = new TrainingWeek(Guid.NewGuid(), profile.Id, weekStart);
                    await weekRepository.AddAsync(week, cancellationToken);
                }

                var activity = Activity.CreateFromStrava(
                    trainingWeekId: week.Id,
                    athleteProfileId: profile.Id,
                    stravaId: dto.StravaId,
                    name: dto.Name,
                    stravaActivityType: dto.SportType,
                    startedAt: dto.StartedAt,
                    durationSeconds: dto.MovingTimeSeconds,
                    distanceMetres: dto.DistanceMetres,
                    averageHeartRate: dto.AverageHeartRate,
                    maxHeartRate: dto.MaxHeartRate,
                    hasHeartRate: dto.HasHeartRate,
                    averagePowerWatts: dto.AveragePowerWatts,
                    isDevicePower: dto.IsDevicePower,
                    averageSpeedMetresPerSecond: dto.AverageSpeedMetresPerSecond);

                // Calculate TSS immediately after creating the activity
                var tss = TryCalculateTss(activity, profile);
                if (tss is not null)
                    activity.AssignTss(tss);

                week.AddActivity(activity);
                await weekRepository.UpdateAsync(week, cancellationToken);

                synced++;
            }

            // Incremental sync: after param returns everything since that time in one call
            if (afterEpoch.HasValue)
                break;

            page++;
        }

        // 7 & 8. Record sync timestamp and save
        profile.RecordSync();
        await profileRepository.UpdateAsync(profile, cancellationToken);

        // 9. Return result
        return new SyncResult(synced, skipped, DateTimeOffset.UtcNow);
    }

    private TssScore? TryCalculateTss(Activity activity, AthleteProfile profile)
    {
        try
        {
            var inputs = BuildTssInputs(activity, profile);
            return tssService.Calculate(inputs);
        }
        catch (DomainException)
        {
            // Insufficient data (e.g. no HR and no power) — TSS stays null until recalculated
            return null;
        }
    }

    private static TssCalculationInputs BuildTssInputs(Activity activity, AthleteProfile profile)
    {
        var duration = ActivityDuration.Create(activity.DurationSeconds);

        HeartRateReading? hrReading = null;
        if (activity.AverageHeartRateBpm is > 0 && activity.MaxHeartRateBpm is > 0)
        {
            try { hrReading = HeartRateReading.Create(activity.AverageHeartRateBpm.Value, activity.MaxHeartRateBpm.Value); }
            catch (DomainException) { /* ignore — HR data out of valid range */ }
        }

        NormalisedPower? np = null;
        if (activity.NormalisedPowerWatts is > 0)
        {
            try { np = NormalisedPower.Create(activity.NormalisedPowerWatts.Value); }
            catch (DomainException) { /* ignore */ }
        }

        return new TssCalculationInputs(
            ActivityType: activity.ActivityType,
            Duration: duration,
            HeartRate: hrReading,
            AthleteHrZones: profile.HeartRateZones,
            NormalisedPower: np,
            Ftp: profile.Ftp,
            Rpe: null);
    }

    /// <summary>Returns the Monday of the ISO week containing the given date.</summary>
    private static DateOnly GetWeekStart(DateOnly date)
    {
        // DayOfWeek: Sun=0, Mon=1, ..., Sat=6
        var dow = (int)date.DayOfWeek;
        var daysToMonday = dow == 0 ? 6 : dow - 1;
        return date.AddDays(-daysToMonday);
    }
}
