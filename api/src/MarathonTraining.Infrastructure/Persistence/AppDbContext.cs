using MarathonTraining.Domain.Aggregates;
using MarathonTraining.Domain.Enums;
using MarathonTraining.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace MarathonTraining.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AthleteProfile> AthleteProfiles => Set<AthleteProfile>();
    public DbSet<StravaConnection> StravaConnections => Set<StravaConnection>();
    public DbSet<TrainingWeek> TrainingWeeks => Set<TrainingWeek>();
    public DbSet<Activity> Activities => Set<Activity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AthleteProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.DisplayName).IsRequired();
            entity.HasIndex(e => e.UserId).IsUnique();

            entity.Property(e => e.CurrentPhase)
                  .HasConversion<string>()
                  .HasDefaultValue(TrainingPhase.Base);

            entity.Property(e => e.LastSyncedAt);

            entity.OwnsOne(e => e.HeartRateZones, hrz =>
            {
                hrz.Property(h => h.RestingHr).HasColumnName("RestingHr");
                hrz.Property(h => h.MaxHr).HasColumnName("MaxHr");
                hrz.Property(h => h.ThresholdHr).HasColumnName("ThresholdHr");
            });

            entity.OwnsOne(e => e.Ftp, ftp =>
            {
                ftp.Property(f => f.Watts).HasColumnName("FtpWatts");
            });
        });

        modelBuilder.Entity<StravaConnection>(entity =>
        {
            entity.HasKey(e => e.AthleteProfileId);
            entity.Property(e => e.AccessToken).IsRequired();
            entity.Property(e => e.RefreshToken).IsRequired();

            entity.HasOne<AthleteProfile>()
                  .WithOne(a => a.StravaConnection)
                  .HasForeignKey<StravaConnection>(e => e.AthleteProfileId);
        });

        modelBuilder.Entity<TrainingWeek>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.AthleteId, e.WeekStartDate }).IsUnique();
            entity.Navigation(e => e.Activities).HasField("_activities");
        });

        modelBuilder.Entity<Activity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.AthleteProfileId);
            entity.HasIndex(e => new { e.AthleteProfileId, e.StartedAt });

            // Filtered unique index — only enforced where StravaActivityId is present
            entity.HasIndex(e => e.StravaActivityId)
                  .IsUnique()
                  .HasFilter("[StravaActivityId] IS NOT NULL");

            entity.Property(e => e.ActivityType).HasConversion<string>();
            entity.Property(e => e.StravaActivityType).HasMaxLength(50);
            entity.Property(e => e.ExternalSource).HasMaxLength(20);

            entity.OwnsOne(e => e.TssScore, tss =>
            {
                tss.Property(t => t.Value).HasColumnName("TssScore").HasColumnType("decimal(8,2)");
            });

            entity.HasOne<TrainingWeek>()
                  .WithMany(w => w.Activities)
                  .HasForeignKey(a => a.TrainingWeekId);
        });
    }
}
