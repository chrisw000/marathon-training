namespace MarathonTraining.Application.Strava;

public interface IStravaOAuthStateService
{
    /// <summary>Generates an opaque state token bound to the given userId and returns it.</summary>
    string GenerateState(string userId);

    /// <summary>Validates and removes the state token, returning the associated userId, or null if invalid/expired.</summary>
    string? ValidateAndConsumeState(string state);
}
