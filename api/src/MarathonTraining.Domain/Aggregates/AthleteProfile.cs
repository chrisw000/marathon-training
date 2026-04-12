using MarathonTraining.Domain.Enums;
using MarathonTraining.Domain.Events;
using MarathonTraining.Domain.ValueObjects;

namespace MarathonTraining.Domain.Aggregates;

public class AthleteProfile
{
    private readonly List<object> _domainEvents = [];

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public StravaConnection? StravaConnection { get; private set; }
    public HeartRateZones? HeartRateZones { get; private set; }
    public FunctionalThresholdPower? Ftp { get; private set; }
    public TrainingPhase CurrentPhase { get; private set; } = TrainingPhase.Base;
    public DateTimeOffset? LastSyncedAt { get; private set; }

    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

    protected AthleteProfile() { }

    public AthleteProfile(Guid id, string userId, string displayName, DateTimeOffset createdAt)
    {
        Id = id;
        UserId = userId;
        DisplayName = displayName;
        CreatedAt = createdAt;
    }

    public void UpdatePhysiology(HeartRateZones zones, FunctionalThresholdPower ftp)
    {
        HeartRateZones = zones;
        Ftp = ftp;
        _domainEvents.Add(new AthletePhysiologyUpdatedEvent(
            Id,
            ftp.Watts,
            zones.RestingHr,
            zones.MaxHr,
            zones.ThresholdHr));
    }

    public void UpdateTrainingPhase(TrainingPhase phase)
    {
        var oldPhase = CurrentPhase;
        CurrentPhase = phase;
        _domainEvents.Add(new TrainingPhaseChangedEvent(Id, oldPhase, phase));
    }

    public void RecordSync()
    {
        LastSyncedAt = DateTimeOffset.UtcNow;
    }
}
