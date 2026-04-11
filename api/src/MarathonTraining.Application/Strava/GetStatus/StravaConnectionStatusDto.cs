namespace MarathonTraining.Application.Strava.GetStatus;

public record StravaConnectionStatusDto(
    bool IsConnected,
    long? StravaAthleteId,
    DateTimeOffset? ExpiresAt);
