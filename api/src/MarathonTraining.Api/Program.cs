using MarathonTraining.Application.Strava;
using MarathonTraining.Application.Strava.Connect;
using MarathonTraining.Application.Strava.Disconnect;
using MarathonTraining.Application.Strava.GetStatus;
using MarathonTraining.Domain.Exceptions;
using MarathonTraining.Domain.Interfaces;
using MarathonTraining.Infrastructure.Persistence;
using MarathonTraining.Infrastructure.Persistence.Repositories;
using MarathonTraining.Infrastructure.Strava;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Authentication — validates JWTs issued by Microsoft Entra External ID
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration, "AzureAd");

// Authorisation — default policy requires an authenticated user
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// OpenAPI
builder.Services.AddOpenApi();

// MediatR — scans Application assembly for all handlers
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(ConnectStravaCommand).Assembly));

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped<IAthleteProfileRepository, AthleteProfileRepository>();
builder.Services.AddScoped<IStravaTokenRepository, StravaTokenRepository>();

// Strava HTTP client — base address is the Strava API root
builder.Services.AddHttpClient<IStravaTokenService, StravaTokenService>(client =>
    client.BaseAddress = new Uri("https://www.strava.com/"));

// Strava OAuth state store — singleton so state survives across requests
builder.Services.AddSingleton<IStravaOAuthStateService, InMemoryStravaOAuthStateService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
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

// ── Strava OAuth ───────────────────────────────────────────────────────────

// Redirects the authenticated user to Strava's OAuth consent page
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

    var stravaAuthUrl =
        $"https://www.strava.com/oauth/authorize" +
        $"?client_id={Uri.EscapeDataString(clientId)}" +
        $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
        $"&response_type=code" +
        $"&scope=activity:read_all" +
        $"&state={Uri.EscapeDataString(state)}";

    return Results.Redirect(stravaAuthUrl);
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

    await sender.Send(new DisconnectStravaCommand(userId));

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

app.Run();

// Expose the implicit Program class so WebApplicationFactory<Program> can reference it from tests.
public partial class Program { }
