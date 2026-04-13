using MarathonTraining.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace MarathonTraining.Infrastructure.Auth;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    // Microsoft.Identity.Web stores the object ID claim under either the short JWT
    // claim name ("oid") or the long WS-Federation URI form, depending on the token
    // handler version and configuration. Check both so the service is resilient to
    // either mapping being in effect.
    private const string OidClaim = "oid";
    private const string ObjectIdClaim = "http://schemas.microsoft.com/identity/claims/objectidentifier";

    public string UserId =>
        httpContextAccessor.HttpContext?.User.FindFirst(ObjectIdClaim)?.Value
        ?? httpContextAccessor.HttpContext?.User.FindFirst(OidClaim)?.Value
        ?? throw new InvalidOperationException("No authenticated user found in the current HTTP context.");

    public string DisplayName =>
        httpContextAccessor.HttpContext?.User.FindFirst("name")?.Value
        ?? UserId;
}
