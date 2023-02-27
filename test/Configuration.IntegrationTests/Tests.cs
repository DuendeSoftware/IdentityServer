using System.Net;
using FluentAssertions;
using IntegrationTests.TestHosts;
using Xunit;

namespace IntegrationTests;

public class Tests : ConfigurationIntegrationTestBase
{
    [Fact]
    public async Task requests_to_dcr_with_incorrect_methods_should_fail()
    {
        var response = await ConfigurationHost.HttpClient!.GetAsync("/connect/dcr");
        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task requests_to_dcr_with_incorrect_content_type_should_fail()
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string> {
            { "redirect_uris", "https://example.com/callback" },
            { "grant_types", "authorization_code" }
        });
        var response = await ConfigurationHost.HttpClient!.PostAsync("/connect/dcr", content);
        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
    }

}




