using System.Net;
using MarathonTraining.Api.IntegrationTests.Support;
using Reqnroll;

namespace MarathonTraining.Api.IntegrationTests.Steps;

[Binding]
public sealed class EndpointAuthSteps(EndpointAuthContext context)
{
    // ── When ─────────────────────────────────────────────────────────────────

    [When("an unauthenticated {word} request is sent to {string}")]
    public async Task WhenAnUnauthenticatedRequestIsSentTo(string method, string route)
    {
        context.LastResponse = method.ToUpperInvariant() switch
        {
            "GET"    => await context.HttpClient!.GetAsync(route),
            "POST"   => await context.HttpClient!.PostAsync(route, content: null),
            "DELETE" => await context.HttpClient!.DeleteAsync(route),
            "PUT"    => await context.HttpClient!.PutAsync(route, content: null),
            "PATCH"  => await context.HttpClient!.PatchAsync(route, content: null),
            _ => throw new ArgumentOutOfRangeException(nameof(method), $"Unsupported HTTP method: {method}"),
        };
    }

    // ── Then ─────────────────────────────────────────────────────────────────

    [Then("the response is 401 Unauthorized")]
    public void ThenTheResponseIs401Unauthorized()
    {
        context.LastResponse!.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
