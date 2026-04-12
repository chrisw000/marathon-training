using MarathonTraining.Domain.Aggregates;
using MarathonTraining.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MarathonTraining.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AthleteProfile> AthleteProfiles => Set<AthleteProfile>();
    public DbSet<StravaConnection> StravaConnections => Set<StravaConnection>();

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
            // AthleteProfileId is both the PK and the FK — one connection per athlete
            entity.HasKey(e => e.AthleteProfileId);
            entity.Property(e => e.AccessToken).IsRequired();
            entity.Property(e => e.RefreshToken).IsRequired();

            entity.HasOne<AthleteProfile>()
                  .WithOne(a => a.StravaConnection)
                  .HasForeignKey<StravaConnection>(e => e.AthleteProfileId);
        });
    }
}
