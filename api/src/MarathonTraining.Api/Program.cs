using Microsoft.AspNetCore.Authorization;
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Health check — anonymous, no auth required
app.MapGet("/health", () => Results.Ok())
   .AllowAnonymous()
   .WithName("Health")
   .WithTags("Diagnostics");

// Returns the authenticated caller's claims — protected by the default policy
app.MapGet("/me", (ClaimsPrincipal user) =>
{
    var claims = user.Claims.Select(c => new { c.Type, c.Value });
    return Results.Ok(claims);
})
.WithName("Me")
.WithTags("Diagnostics");

app.Run();
