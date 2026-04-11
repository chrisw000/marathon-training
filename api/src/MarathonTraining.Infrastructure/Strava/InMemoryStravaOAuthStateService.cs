using System.Collections.Concurrent;
using MarathonTraining.Application.Strava;

namespace MarathonTraining.Infrastructure.Strava;

/// <summary>
/// In-memory OAuth state store. Sufficient for single-instance deployments and development.
/// Replace with a distributed cache (Redis) for multi-instance production deployments.
/// </summary>
public sealed class InMemoryStravaOAuthStateService : IStravaOAuthStateService
{
    private readonly ConcurrentDictionary<string, (string UserId, DateTimeOffset ExpiresAt)> _states = new();

    private static readonly TimeSpan StateTtl = TimeSpan.FromMinutes(10);

    public string GenerateState(string userId)
    {
        var state = Guid.NewGuid().ToString("N");
        _states[state] = (userId, DateTimeOffset.UtcNow.Add(StateTtl));
        return state;
    }

    public string? ValidateAndConsumeState(string state)
    {
        if (!_states.TryRemove(state, out var entry))
            return null;

        return entry.ExpiresAt > DateTimeOffset.UtcNow ? entry.UserId : null;
    }
}
