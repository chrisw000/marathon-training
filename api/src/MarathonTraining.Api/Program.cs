using FluentValidation;
using MarathonTraining.Application.Athlete;
using MarathonTraining.Application.Common.Behaviours;
using MarathonTraining.Application.Common.Interfaces;
using MarathonTraining.Application.Profile;
using MarathonTraining.Application.Strava;
using MarathonTraining.Application.Strava.Connect;
using MarathonTraining.Application.Strava.Disconnect;
using MarathonTraining.Application.Strava.GetStatus;
using MarathonTraining.Api.Endpoints;
using MarathonTraining.Domain.Exceptions;
using MarathonTraining.Domain.Interfaces;
using MarathonTraining.Infrastructure.Auth;
using MarathonTraining.Infrastructure.Persistence;
using MarathonTraining.Infrastructure.Persistence.Repositories;
using MarathonTraining.Infrastructure.Strava;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Authentication — validates JWTs issued by Microsoft Entra External ID
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration, "AzureAd");

// Entra tokens carry aud = "api://<guid>" but AzureAd:Audience is typically configured as
// just "<guid>" (without the api:// prefix). PostConfigure runs after Microsoft.Identity.Web
// has already built ValidAudiences, so we can safely append the api:// form here.
builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    var audience = builder.Configuration["AzureAd:Audience"]
                   ?? builder.Configuration["AzureAd:ClientId"];
    if (string.IsNullOrEmpty(audience)) return;

    var audiences = options.TokenValidationParameters.ValidAudiences?.ToList() ?? [];
    foreach (var form in (string[])[audience, $"api://{audience}"])
    {
        if (!audiences.Contains(form))
            audiences.Add(form);
    }
    options.TokenValidationParameters.ValidAudiences = audiences;
});

// Authorisation — default policy requires an authenticated user
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// CORS — permit the Vite dev server to call the API directly
builder.Services.AddCors();

// OpenAPI
builder.Services.AddOpenApi();

// MediatR — scans Application assembly for all handlers + validation pipeline
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(ConnectStravaCommand).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehaviour<,>));
});

// FluentValidation — registers all validators in the Application assembly
builder.Services.AddValidatorsFromAssembly(typeof(GetAthleteProfileQuery).Assembly);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped<IAthleteProfileRepository, AthleteProfileRepository>();
builder.Services.AddScoped<IStravaTokenRepository, StravaTokenRepository>();

// Current user service — reads oid claim from the HTTP context
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Strava HTTP client — base address is the Strava API root
builder.Services.AddHttpClient<IStravaTokenService, StravaTokenService>(client =>
    client.BaseAddress = new Uri("https://www.strava.com/"));

// Strava OAuth state store — singleton so state survives across requests
builder.Services.AddSingleton<IStravaOAuthStateService, InMemoryStravaOAuthStateService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Allow the Vite dev server to call the API without CORS errors.
    // Origins are in appsettings.Development.json so Vite's port auto-increment
    // (5173, 5174, ...) doesn't require a code change.
    var allowedOrigins = app.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? ["http://localhost:5173"];
    app.UseCors(policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod());

    // Create the schema on startup in Development so there is no need to run migrations
    // or apply them manually. EnsureCreatedAsync is a no-op if the schema already exists.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// ── Diagnostics ────────────────────────────────────────────────────────────

app.MapGet("/health", () => Results.Ok())
   .AllowAnonymous()
   .WithName("Health")
   .WithTags("Diagnostics");

app.MapGet("/me", (ClaimsPrincipal user) =>
{
    var claims = user.Claims.Select(c => new { c.Type, c.Value });
    return Results.Ok(claims);
})
.WithName("Me")
.WithTags("Diagnostics");

// ── Profile ────────────────────────────────────────────────────────────────

// Idempotent — creates the AthleteProfile for the current user if one does not exist yet.
// Safe to call on every login; the UI calls this automatically after authentication.
app.MapPost("/api/profile", async (ClaimsPrincipal user, ISender sender) =>
{
    var userId = user.GetObjectId()
        ?? throw new InvalidOperationException("No object ID claim found on the authenticated user.");

    var displayName = user.Identity?.Name ?? userId;
    var result = await sender.Send(new EnsureAthleteProfileCommand(userId, displayName));

    return Results.Ok(new { created = result.WasCreated });
})
.WithName("EnsureProfile")
.WithTags("Profile");

// ── Strava OAuth ───────────────────────────────────────────────────────────

// Returns the Strava OAuth consent URL. The client navigates to it via window.location —
// a server-side redirect cannot be followed by a fetch call that carries a Bearer token.
app.MapGet("/api/strava/authorise", (
    ClaimsPrincipal user,
    IStravaOAuthStateService stateService,
    IConfiguration config) =>
{
    var userId = user.GetObjectId()
        ?? throw new InvalidOperationException("No object ID claim found on the authenticated user.");

    var state = stateService.GenerateState(userId);
    var clientId = config["Strava:ClientId"]
        ?? throw new InvalidOperationException("Strava:ClientId is not configured.");
    var redirectUri = config["Strava:RedirectUri"]
        ?? throw new InvalidOperationException("Strava:RedirectUri is not configured.");

    var url =
        $"https://www.strava.com/oauth/authorize" +
        $"?client_id={Uri.EscapeDataString(clientId)}" +
        $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
        $"&response_type=code" +
        $"&scope=activity:read_all" +
        $"&state={Uri.EscapeDataString(state)}";

    return Results.Ok(new { url });
})
.WithName("StravaAuthorise")
.WithTags("Strava");

// Strava posts back here with the authorisation code — anonymous because the browser redirects here
app.MapGet("/api/strava/callback", async (
    string? code,
    string? state,
    string? error,
    IStravaOAuthStateService stateService,
    ISender sender,
    IConfiguration config) =>
{
    var uiBase = config["Strava:UiSuccessRedirectUri"] ?? "http://localhost:5173/strava-connected";

    if (error is not null || code is null || state is null)
        return Results.Redirect($"{uiBase}?error=access_denied");

    var userId = stateService.ValidateAndConsumeState(state);
    if (userId is null)
        return Results.Redirect($"{uiBase}?error=invalid_state");

    try
    {
        await sender.Send(new ConnectStravaCommand(userId, code));
    }
    catch (DomainException)
    {
        return Results.BadRequest(new { error = "invalid_code" });
    }

    return Results.Redirect(uiBase);
})
.AllowAnonymous()
.WithName("StravaCallback")
.WithTags("Strava");

// Removes the stored Strava tokens for the current athlete
app.MapDelete("/api/strava/disconnect", async (ClaimsPrincipal user, ISender sender) =>
{
    var userId = user.GetObjectId()
        ?? throw new InvalidOperationException("No object ID claim found on the authenticated user.");

    try
    {
        await sender.Send(new DisconnectStravaCommand(userId));
    }
    catch (DomainException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }

    return Results.NoContent();
})
.WithName("StravaDisconnect")
.WithTags("Strava");

// Returns the Strava connection status for the current athlete
app.MapGet("/api/strava/status", async (ClaimsPrincipal user, ISender sender) =>
{
    var userId = user.GetObjectId()
        ?? throw new InvalidOperationException("No object ID claim found on the authenticated user.");

    var status = await sender.Send(new GetStravaConnectionStatusQuery(userId));

    return Results.Ok(status);
})
.WithName("StravaStatus")
.WithTags("Strava");

// ── Athlete ────────────────────────────────────────────────────────────────

app.MapAthleteEndpoints();

app.Run();

// Expose the implicit Program class so WebApplicationFactory<Program> can reference it from tests.
public partial class Program { }
