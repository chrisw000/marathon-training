namespace MarathonTraining.Domain.Aggregates;

public class TrainingWeek
{
    private readonly List<Activity> _activities = [];

    public Guid Id { get; private set; }
    public Guid AthleteId { get; private set; }
    public DateOnly WeekStartDate { get; private set; }
    public IReadOnlyCollection<Activity> Activities => _activities.AsReadOnly();

    protected TrainingWeek() { }

    public TrainingWeek(Guid id, Guid athleteId, DateOnly weekStartDate)
    {
        Id = id;
        AthleteId = athleteId;
        WeekStartDate = weekStartDate;
    }
}
