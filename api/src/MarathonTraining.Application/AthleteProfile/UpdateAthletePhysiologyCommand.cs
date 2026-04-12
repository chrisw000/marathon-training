using FluentValidation;
using MarathonTraining.Application.Common.Exceptions;
using MarathonTraining.Application.Common.Interfaces;
using MarathonTraining.Domain.Interfaces;
using MarathonTraining.Domain.ValueObjects;
using MediatR;

namespace MarathonTraining.Application.Athlete;

public record UpdateAthletePhysiologyCommand(
    int RestingHr,
    int MaxHr,
    int ThresholdHr,
    int FtpWatts)
    : IRequest<AthleteProfileResponse>;

public sealed class UpdateAthletePhysiologyCommandValidator
    : AbstractValidator<UpdateAthletePhysiologyCommand>
{
    public UpdateAthletePhysiologyCommandValidator()
    {
        RuleFor(x => x.RestingHr)
            .GreaterThan(0).WithMessage("RestingHr must be greater than zero.");

        RuleFor(x => x.MaxHr)
            .GreaterThan(0).WithMessage("MaxHr must be greater than zero.")
            .LessThanOrEqualTo(220).WithMessage("MaxHr must not exceed 220.");

        RuleFor(x => x.ThresholdHr)
            .GreaterThan(0).WithMessage("ThresholdHr must be greater than zero.")
            .Must((cmd, threshold) => threshold < cmd.MaxHr)
            .WithMessage("ThresholdHr must be less than MaxHr.");

        RuleFor(x => x.FtpWatts)
            .GreaterThan(0).WithMessage("FtpWatts must be greater than zero.")
            .LessThan(600).WithMessage("FtpWatts must be less than 600.");
    }
}

public sealed class UpdateAthletePhysiologyCommandHandler(
    IAthleteProfileRepository repository,
    ICurrentUserService currentUser)
    : IRequestHandler<UpdateAthletePhysiologyCommand, AthleteProfileResponse>
{
    public async Task<AthleteProfileResponse> Handle(
        UpdateAthletePhysiologyCommand request,
        CancellationToken cancellationToken)
    {
        var profile = await repository.GetByUserIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException($"No athlete profile found for user '{currentUser.UserId}'.");

        var zones = HeartRateZones.Create(request.RestingHr, request.MaxHr, request.ThresholdHr);
        var ftp = FunctionalThresholdPower.Create(request.FtpWatts);

        profile.UpdatePhysiology(zones, ftp);
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
