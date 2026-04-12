using FluentValidation;
using MarathonTraining.Application.Athlete;
using MarathonTraining.Application.Common.Exceptions;
using MarathonTraining.Domain.Exceptions;
using MediatR;

namespace MarathonTraining.Api.Endpoints;

public static class AthleteEndpoints
{
    public static WebApplication MapAthleteEndpoints(this WebApplication app)
    {
        app.MapGet("/api/athlete/profile", async (ISender sender) =>
        {
            try
            {
                var result = await sender.Send(new GetAthleteProfileQuery());
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
        .WithName("GetAthleteProfile")
        .WithTags("Athlete");

        app.MapPatch("/api/athlete/physiology", async (
            UpdateAthletePhysiologyCommand command,
            ISender sender) =>
        {
            try
            {
                var result = await sender.Send(command);
                return Results.Ok(result);
            }
            catch (ValidationException ex)
            {
                return Results.ValidationProblem(
                    ex.Errors.ToDictionary(
                        e => e.PropertyName,
                        e => new[] { e.ErrorMessage }));
            }
            catch (DomainException ex)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        ["domain"] = [ex.Message]
                    });
            }
            catch (NotFoundException ex)
            {
                return Results.Problem(
                    title: "Athlete profile not found.",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status404NotFound);
            }
        })
        .WithName("UpdateAthletePhysiology")
        .WithTags("Athlete");

        app.MapPatch("/api/athlete/phase", async (
            UpdateTrainingPhaseCommand command,
            ISender sender) =>
        {
            try
            {
                var result = await sender.Send(command);
                return Results.Ok(result);
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
        .WithName("UpdateTrainingPhase")
        .WithTags("Athlete");

        return app;
    }
}
