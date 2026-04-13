namespace MarathonTraining.Infrastructure.Strava.Exceptions;

/// <summary>Thrown when the Strava API returns an unexpected 4xx or 5xx status.</summary>
public class StravaApiException(int statusCode, string message)
    : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

/// <summary>Thrown when Strava returns 401 — the access token has expired.</summary>
public class StravaUnauthorisedException()
    : StravaApiException(401, "Strava access token is invalid or expired.");

/// <summary>Thrown when Strava returns 429 — the rate limit has been hit.</summary>
public class StravaRateLimitException()
    : StravaApiException(429, "Strava API rate limit exceeded. Back off before retrying.");
