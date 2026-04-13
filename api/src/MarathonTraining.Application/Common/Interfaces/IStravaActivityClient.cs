using MarathonTraining.Application.Activities;

namespace MarathonTraining.Application.Common.Interfaces;

public interface IStravaActivityClient
{
    Task<IReadOnlyList<StravaActivitySummary>> GetActivitiesAsync(
        string accessToken,
        long? afterEpoch,
        int page,
        int perPage = 100,
        CancellationToken cancellationToken = default);
}
