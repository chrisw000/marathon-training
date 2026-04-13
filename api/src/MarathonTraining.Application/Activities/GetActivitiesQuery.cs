using MarathonTraining.Application.Common.Exceptions;
using MarathonTraining.Application.Common.Interfaces;
using MarathonTraining.Domain.Enums;
using MarathonTraining.Domain.Interfaces;
using MediatR;

namespace MarathonTraining.Application.Activities;

public record GetActivitiesQuery(
    ActivityType? Type,
    DateOnly? From,
    DateOnly? To,
    int Page,
    int PageSize)
    : IRequest<PagedResult<ActivitySummaryResponse>>;

public record ActivitySummaryResponse(
    Guid Id,
    string Name,
    string ActivityType,
    DateTimeOffset StartedAt,
    int DurationSeconds,
    double? DistanceMetres,
    decimal? TssScore,
    int? AverageHeartRateBpm,
    int? RpeValue,
    long? StravaActivityId,
    string? ExternalSource);

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

public sealed class GetActivitiesQueryHandler(
    IAthleteProfileRepository profileRepository,
    IActivityRepository activityRepository,
    ICurrentUserService currentUser)
    : IRequestHandler<GetActivitiesQuery, PagedResult<ActivitySummaryResponse>>
{
    public async Task<PagedResult<ActivitySummaryResponse>> Handle(
        GetActivitiesQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await profileRepository.GetByUserIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException($"Athlete profile for user '{currentUser.UserId}' not found.");

        var from = request.From ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-365));
        var to = request.To ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var activities = await activityRepository.GetByAthleteAndDateRangeAsync(
            profile.Id, from, to, cancellationToken);

        var filtered = activities.AsEnumerable();

        if (request.Type.HasValue)
            filtered = filtered.Where(a => a.ActivityType == request.Type.Value);

        var ordered = filtered
            .OrderByDescending(a => a.StartedAt)
            .ToList();

        var totalCount = ordered.Count;
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var page = Math.Max(1, request.Page);

        var items = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new ActivitySummaryResponse(
                Id: a.Id,
                Name: a.Name,
                ActivityType: a.ActivityType.ToString(),
                StartedAt: a.StartedAt,
                DurationSeconds: a.DurationSeconds,
                DistanceMetres: a.DistanceMetres,
                TssScore: a.TssScore?.Value,
                AverageHeartRateBpm: a.AverageHeartRateBpm,
                RpeValue: a.RpeValue,
                StravaActivityId: a.StravaActivityId,
                ExternalSource: a.ExternalSource))
            .ToList();

        return new PagedResult<ActivitySummaryResponse>(items, totalCount, page, pageSize);
    }
}
