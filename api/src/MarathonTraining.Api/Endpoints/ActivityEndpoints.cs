using FluentValidation;
using MarathonTraining.Application.Activities;
using MarathonTraining.Application.Common.Exceptions;
using MarathonTraining.Domain.Enums;
using MarathonTraining.Infrastructure.Strava.Exceptions;
using MediatR;

namespace MarathonTraining.Api.Endpoints;

public static class ActivityEndpoints
{
    public static WebApplication MapActivityEndpoints(this WebApplication app)
    {
        // Trigger an incremental (or full initial) Strava activity sync
        app.MapPost("/api/activities/sync", async (ISender sender) =>
        {
            try
            {
                var result = await sender.Send(new SyncStravaActivitiesCommand());
                return Results.Ok(result);
            }
            catch (StravaNotConnectedException ex)
            {
                return Results.Problem(
                    title: "Strava not connected.",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status422UnprocessableEntity);
            }
            catch (StravaRateLimitException ex)
            {
                return Results.Problem(
                    title: "Strava rate limit exceeded.",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status429TooManyRequests);
            }
            catch (NotFoundException ex)
            {
                return Results.Problem(
                    title: "Athlete profile not found.",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status404NotFound);
            }
        })
        .WithName("SyncStravaActivities")
        .WithTags("Activities");

        // Log a manual strength activity
        app.MapPost("/api/activities/manual", async (
            LogManualActivityCommand command,
            ISender sender) =>
        {
            try
            {
                var result = await sender.Send(command);
                return Results.Created($"/api/activities/{result.ActivityId}", result);
            }
            catch (ValidationException ex)
            {
                return Results.ValidationProblem(
                    ex.Errors.ToDictionary(
                        e => e.PropertyName,
                        e => new[] { e.ErrorMessage }));
            }
            catch (NotFoundException ex)
            {
                return Results.Problem(
                    title: "Athlete profile not found.",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status404NotFound);
            }
        })
        .WithName("LogManualActivity")
        .WithTags("Activities");

        // Paginated activity list — optional filters: type, from, to, page, pageSize
        app.MapGet("/api/activities", async (
            string? type,
            string? from,
            string? to,
            int page = 1,
            int pageSize = 20,
            ISender sender = default!) =>
        {
            ActivityType? activityType = null;
            if (type is not null)
            {
                if (!Enum.TryParse<ActivityType>(type, ignoreCase: true, out var parsed))
                    return Results.Problem(
                        title: "Invalid activity type.",
                        detail: $"'{type}' is not a recognised activity type. Use Run, Ride, or Strength.",
                        statusCode: StatusCodes.Status400BadRequest);
                activityType = parsed;
            }

            DateOnly? fromDate = null;
            if (from is not null)
            {
                if (!DateOnly.TryParse(from, out var parsedFrom))
                    return Results.Problem(
                        title: "Invalid date format.",
                        detail: "Parameter 'from' must be a valid date (yyyy-MM-dd).",
                        statusCode: StatusCodes.Status400BadRequest);
                fromDate = parsedFrom;
            }

            DateOnly? toDate = null;
            if (to is not null)
            {
                if (!DateOnly.TryParse(to, out var parsedTo))
                    return Results.Problem(
                        title: "Invalid date format.",
                        detail: "Parameter 'to' must be a valid date (yyyy-MM-dd).",
                        statusCode: StatusCodes.Status400BadRequest);
                toDate = parsedTo;
            }

            try
            {
                var result = await sender.Send(
                    new GetActivitiesQuery(activityType, fromDate, toDate, page, pageSize));
                return Results.Ok(result);
            }
            catch (NotFoundException ex)
            {
                return Results.Problem(
                    title: "Athlete profile not found.",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status404NotFound);
            }
        })
        .WithName("GetActivities")
        .WithTags("Activities");

        return app;
    }
}
