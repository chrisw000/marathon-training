using MarathonTraining.Infrastructure.Auth;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using System.Security.Claims;

namespace MarathonTraining.Infrastructure.Tests.Auth;

public sealed class CurrentUserServiceTests
{
    private readonly IHttpContextAccessor _httpContextAccessor =
        Substitute.For<IHttpContextAccessor>();

    private CurrentUserService CreateService() => new(_httpContextAccessor);

    private void SetUser(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = Substitute.For<HttpContext>();
        httpContext.User.Returns(principal);
        _httpContextAccessor.HttpContext.Returns(httpContext);
    }

    // ── UserId ────────────────────────────────────────────────────────────────

    [Fact]
    public void UserId_ShortOidClaim_ReturnsValue()
    {
        var objectId = Guid.NewGuid().ToString();
        SetUser(new Claim("oid", objectId));

        CreateService().UserId.Should().Be(objectId);
    }

    [Fact]
    public void UserId_UriFormObjectIdentifierClaim_ReturnsValue()
    {
        // Microsoft.Identity.Web can store the OID under the long WS-Federation URI
        // form depending on the token handler version. This must also be recognised.
        var objectId = Guid.NewGuid().ToString();
        SetUser(new Claim(
            "http://schemas.microsoft.com/identity/claims/objectidentifier",
            objectId));

        CreateService().UserId.Should().Be(objectId);
    }

    [Fact]
    public void UserId_UriFormTakesPrecedenceOverShortForm()
    {
        // If both claim names are present, the URI form is authoritative.
        var uriObjectId = Guid.NewGuid().ToString();
        var shortObjectId = Guid.NewGuid().ToString();
        SetUser(
            new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", uriObjectId),
            new Claim("oid", shortObjectId));

        CreateService().UserId.Should().Be(uriObjectId);
    }

    [Fact]
    public void UserId_NoOidClaim_ThrowsInvalidOperationException()
    {
        SetUser(new Claim("sub", "some-subject"));

        var sut = CreateService();
        var act = () => sut.UserId;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*No authenticated user*");
    }

    [Fact]
    public void UserId_NullHttpContext_ThrowsInvalidOperationException()
    {
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        var sut = CreateService();
        var act = () => sut.UserId;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*No authenticated user*");
    }

    // ── DisplayName ───────────────────────────────────────────────────────────

    [Fact]
    public void DisplayName_NameClaimPresent_ReturnsName()
    {
        SetUser(
            new Claim("oid", Guid.NewGuid().ToString()),
            new Claim("name", "Chris W"));

        CreateService().DisplayName.Should().Be("Chris W");
    }

    [Fact]
    public void DisplayName_NoNameClaim_FallsBackToUserId()
    {
        var objectId = Guid.NewGuid().ToString();
        SetUser(new Claim("oid", objectId));

        CreateService().DisplayName.Should().Be(objectId);
    }
}
