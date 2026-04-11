using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarathonTraining.Api.IntegrationTests.Support;

/// <summary>
/// Test-only authentication handler. Reads the <see cref="UserIdHeader"/> request header
/// and creates an authenticated <see cref="ClaimsPrincipal"/> from it, bypassing real JWT
/// validation. If the header is absent the request is treated as anonymous.
/// </summary>
public sealed class FakeAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";

    /// <summary>
    /// Set this header on test requests to authenticate as a specific user.
    /// The value becomes the <c>oid</c> claim, which is what <c>GetObjectId()</c> reads.
    /// </summary>
    public const string UserIdHeader = "X-Test-User-Id";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserIdHeader, out var values)
            || string.IsNullOrEmpty(values))
            return Task.FromResult(AuthenticateResult.NoResult());

        var userId = values.ToString();
        var claims = new[] { new Claim("oid", userId) };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
