namespace MarathonTraining.Domain.Aggregates;

public class AthleteProfile
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    protected AthleteProfile() { }

    public AthleteProfile(Guid id, string userId, string displayName, DateTimeOffset createdAt)
    {
        Id = id;
        UserId = userId;
        DisplayName = displayName;
        CreatedAt = createdAt;
    }
}
