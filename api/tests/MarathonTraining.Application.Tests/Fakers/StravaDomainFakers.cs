using Bogus;
using MarathonTraining.Application.Strava;
using MarathonTraining.Domain.Aggregates;

namespace MarathonTraining.Application.Tests.Fakers;

internal static class StravaDomainFakers
{
    private static readonly Faker Fake = new();

    internal static AthleteProfile AthleteProfile(string? userId = null) =>
        new(
            id: Guid.NewGuid(),
            userId: userId ?? Fake.Internet.UserName(),
            displayName: Fake.Name.FullName(),
            createdAt: DateTimeOffset.UtcNow.AddDays(-Fake.Random.Int(1, 365)));

    internal static StravaConnection StravaConnection(
        Guid? athleteProfileId = null,
        DateTimeOffset? expiresAt = null) =>
        new(
            athleteProfileId: athleteProfileId ?? Guid.NewGuid(),
            stravaAthleteId: Fake.Random.Long(1_000_000, 9_999_999),
            accessToken: Fake.Random.AlphaNumeric(40),
            refreshToken: Fake.Random.AlphaNumeric(40),
            expiresAt: expiresAt ?? DateTimeOffset.UtcNow.AddHours(Fake.Random.Int(1, 6)));

    internal static StravaTokenResponse StravaTokenResponse(DateTimeOffset? expiresAt = null) =>
        new(
            AccessToken: Fake.Random.AlphaNumeric(40),
            RefreshToken: Fake.Random.AlphaNumeric(40),
            ExpiresAt: expiresAt ?? DateTimeOffset.UtcNow.AddHours(6),
            StravaAthleteId: Fake.Random.Long(1_000_000, 9_999_999));
}
