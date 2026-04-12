# Testing Patterns

---

## Project map

| Project | Tests | Tools |
|---|---|---|
| `MarathonTraining.Application.Tests` | MediatR handler unit tests — all business logic paths | xUnit, NSubstitute, AwesomeAssertions, Bogus |
| `MarathonTraining.Api.IntegrationTests` | Full HTTP stack BDD scenarios — DB + WireMock | Reqnroll, xUnit, WireMock.Net, Testcontainers.MsSql, AwesomeAssertions |
| `MarathonTraining.Domain.Tests` | Domain value object and aggregate behaviour | xUnit, AwesomeAssertions (placeholder — add tests here) |
| `MarathonTraining.Infrastructure.Tests` | Repository implementations against real DB | xUnit (placeholder) |

---

## xUnit test class and method naming

**Class:** `{SubjectUnderTest}Tests`  
**Method:** `{MethodName}_{Condition}_{ExpectedOutcome}`

```csharp
public sealed class ConnectStravaCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidAuthCode_StoresTokens() { ... }

    [Fact]
    public async Task Handle_TokenExchangeFails_ThrowsDomainException() { ... }
}
```

Classes are `sealed`. The system-under-test field is conventionally named `_sut`.

---

## NSubstitute mock setup pattern

Dependencies are created as fields via `Substitute.For<T>()`. The SUT is constructed in the
constructor (not in `[Fact]` methods), so all tests share the same mock instances.

```csharp
private readonly IAthleteProfileRepository _athleteProfileRepository =
    Substitute.For<IAthleteProfileRepository>();

private readonly ConnectStravaCommandHandler _sut;

public ConnectStravaCommandHandlerTests()
{
    _sut = new ConnectStravaCommandHandler(
        _athleteProfileRepository,
        _stravaTokenRepository,
        _stravaTokenService);
}
```

**Setting up returns:**
```csharp
_athleteProfileRepository
    .GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>())
    .Returns(profile);
```

**Throwing from async methods:**
```csharp
_stravaTokenService
    .ExchangeCodeAsync(command.AuthCode, Arg.Any<CancellationToken>())
    .ThrowsAsync(new HttpRequestException("Strava returned 401 Unauthorized."));
```

**Verifying calls:**
```csharp
await _stravaTokenRepository.Received(1).UpsertAsync(
    Arg.Is<StravaConnection>(c =>
        c.AthleteProfileId == profile.Id &&
        c.AccessToken == tokenResponse.AccessToken),
    Arg.Any<CancellationToken>());

await _profileRepository.DidNotReceive().AddAsync(
    Arg.Any<AthleteProfile>(),
    Arg.Any<CancellationToken>());
```

---

## Bogus faker pattern

**Static factory class per domain area** in `Application.Tests/Fakers/`.  
Uses a shared `Faker` instance (not `Faker<T>` fluent builder) with static factory methods.

**Real example — `StravaDomainFakers`:**
```csharp
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
}
```

**Convention for new features:**
- Add a new static class to `Application.Tests/Fakers/` (e.g. `TrainingWeekFakers`)
- Or add factory methods to `StravaDomainFakers` if the entity is Strava-related
- Nullable overrides for all fields that tests commonly need to control (e.g. `userId`, `expiresAt`)

When a test only needs one or two specific fields and doesn't warrant a faker, using
`new Faker()` inline is acceptable:
```csharp
private static readonly Faker Fake = new();
var userId = Guid.NewGuid().ToString();
var displayName = Fake.Name.FullName();
```

---

## AwesomeAssertions style

AwesomeAssertions is a drop-in replacement for FluentAssertions with an MIT license.
The API is identical to FluentAssertions.

```csharp
// Value equality
result.WasCreated.Should().BeTrue();
result.IsConnected.Should().BeFalse();
connection.ExpiresAt.Should().Be(expiresAt);

// Null checks
profile.Should().NotBeNull("an AthleteProfile was seeded for this scenario");
result.StravaAthleteId.Should().BeNull();

// String wildcards
await act.Should().ThrowAsync<DomainException>()
    .WithMessage("*authorisation code*");

// HTTP assertions
context.LastResponse!.StatusCode.Should().Be(HttpStatusCode.Found);
context.LastResponse.Headers.Location
    .Should().Be(new Uri("http://localhost:5173/strava-connected"));
```

`AwesomeAssertions` and `Xunit` are added as global usings in the test `.csproj` files,
so no `using` directives are needed in test files.

---

## Reqnroll feature file conventions

**Location:** `api/tests/MarathonTraining.Api.IntegrationTests/Features/`  
**Naming:** `{FeatureName}.feature` (PascalCase, noun-based: `StravaAuth.feature`, `AthleteProfile.feature`)

**Structure:**
```gherkin
Feature: {Feature Name in title case}

  Scenario: {Happy path description}
    Given {precondition}
    When {action}
    Then {assertion}
    And {additional assertion}

  Scenario: {Error path description}
    Given {precondition}
    When {action}
    Then {error assertion}
```

**Step binding classes:** `Steps/{FeatureName}Steps.cs` — `sealed` class, constructor-injected context
**Context class:** `Support/{FeatureName}Context.cs` — plain POCO with nullable properties
**Hooks class:** `Hooks/{FeatureName}Hooks.cs` — `[BeforeScenario]` / `[AfterScenario]`

The `[BeforeScenario]` hook:
1. Creates `ApiWebApplicationFactory` with the scenario's WireMock URL and `ContainerHooks.ConnectionString`
2. Creates `HttpClient` with `AllowAutoRedirect = false`
3. Wipes DB tables (FK-safe order: child rows first)

---

## WireMock setup pattern

WireMock is started in `[BeforeScenario]` and stopped in `[AfterScenario]`:
```csharp
context.WireMockServer = WireMockServer.Start();
// URL: context.WireMockServer.Url! — passed to ApiWebApplicationFactory
```

Stubs are set up **inside the `[When]` step** (not in Given), immediately before the HTTP call:
```csharp
context.WireMockServer!
    .Given(Request.Create().WithPath("/oauth/token").UsingPost())
    .RespondWith(Response.Create()
        .WithStatusCode(200)
        .WithHeader("Content-Type", "application/json")
        .WithBody(tokenResponseBody));
```

The `ApiWebApplicationFactory` replaces the `IStravaTokenService` HttpClient's base address
with `context.WireMockServer.Url`, so all `POST /oauth/token` calls hit WireMock.

---

## WebApplicationFactory usage pattern

`ApiWebApplicationFactory : WebApplicationFactory<Program>` lives in `Support/`.

It replaces three registrations in `ConfigureTestServices`:
1. `AppDbContext` — redirected to the Testcontainer SQL Server via `_testConnectionString`
2. `IStravaTokenService` HttpClient — base address redirected to `_stravaBaseUrl` (WireMock)
3. JWT Bearer auth — replaced with `FakeAuthHandler` (reads `X-Test-User-Id` request header)

**Creating the factory:**
```csharp
context.Factory = new ApiWebApplicationFactory(
    stravaBaseUrl: context.WireMockServer.Url!,
    testConnectionString: ContainerHooks.ConnectionString);

context.HttpClient = context.Factory.CreateClient(new WebApplicationFactoryClientOptions
{
    AllowAutoRedirect = false,
});
```

**For tests that don't touch Strava**, pass a dummy URL:
```csharp
context.Factory = new ApiWebApplicationFactory(
    stravaBaseUrl: "http://localhost:9999",
    testConnectionString: ContainerHooks.ConnectionString);
```

**Authenticated requests:** Set `X-Test-User-Id` header on `context.HttpClient.DefaultRequestHeaders`.
`FakeAuthHandler` turns this into a `ClaimsPrincipal` with an `oid` claim.

**DB access in steps:** Always use a fresh `IServiceScope` — never reuse the factory scope:
```csharp
await using var scope = context.Factory!.Services.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
```

---

## Testcontainers setup

One SQL Server container is started per test run (`[BeforeTestRun]`) in `ContainerHooks`.
The container uses `mcr.microsoft.com/azure-sql-edge:latest` (multi-arch, ARM64-compatible).

The wait strategy is `Wait.ForUnixContainer().UntilPortIsAvailable(1433)` (not the default
`sqlcmd` check, which fails on Azure SQL Edge). After the port is open, `EnsureCreatedAsync`
is retried up to 30 times with 2-second delays to handle the engine startup window.
