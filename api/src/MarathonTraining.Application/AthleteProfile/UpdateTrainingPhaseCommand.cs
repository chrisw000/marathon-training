using FluentValidation;
using MarathonTraining.Application.Common.Exceptions;
using MarathonTraining.Application.Common.Interfaces;
using MarathonTraining.Domain.Enums;
using MarathonTraining.Domain.Interfaces;
using MediatR;

namespace MarathonTraining.Application.Athlete;

public record UpdateTrainingPhaseCommand(string Phase) : IRequest<AthleteProfileResponse>;

public sealed class UpdateTrainingPhaseCommandValidator : AbstractValidator<UpdateTrainingPhaseCommand>
{
    private static readonly string[] ValidPhases =
        Enum.GetNames<TrainingPhase>();

    public UpdateTrainingPhaseCommandValidator()
    {
        RuleFor(x => x.Phase)
            .NotEmpty().WithMessage("Phase is required.")
            .Must(p => Enum.TryParse<TrainingPhase>(p, out _))
            .WithMessage($"Phase must be one of: {string.Join(", ", ValidPhases)}.");
    }
}

public sealed class UpdateTrainingPhaseCommandHandler(
    IAthleteProfileRepository repository,
    ICurrentUserService currentUser)
    : IRequestHandler<UpdateTrainingPhaseCommand, AthleteProfileResponse>
{
    public async Task<AthleteProfileResponse> Handle(
        UpdateTrainingPhaseCommand request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<TrainingPhase>(request.Phase, out var phase))
            throw new ValidationException($"Phase must be one of: {string.Join(", ", Enum.GetNames<TrainingPhase>())}.");

        var profile = await repository.GetByUserIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException($"No athlete profile found for user '{currentUser.UserId}'.");

        profile.UpdateTrainingPhase(phase);
        await repository.UpdateAsync(profile, cancellationToken);

        return new AthleteProfileResponse(
            Id: profile.Id,
            DisplayName: profile.DisplayName,
            RestingHr: profile.HeartRateZones?.RestingHr,
            MaxHr: profile.HeartRateZones?.MaxHr,
            ThresholdHr: profile.HeartRateZones?.ThresholdHr,
            FtpWatts: profile.Ftp?.Watts,
            CurrentPhase: profile.CurrentPhase.ToString(),
            HasStravaConnected: profile.StravaConnection is not null,
            LastSyncedAt: profile.LastSyncedAt);
    }
}
