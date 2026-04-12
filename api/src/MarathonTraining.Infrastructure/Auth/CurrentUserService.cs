using MarathonTraining.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace MarathonTraining.Infrastructure.Auth;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public string UserId =>
        httpContextAccessor.HttpContext?.User.FindFirst("oid")?.Value
        ?? throw new InvalidOperationException("No authenticated user found in the current HTTP context.");

    public string DisplayName =>
        httpContextAccessor.HttpContext?.User.FindFirst("name")?.Value
        ?? UserId;
}
