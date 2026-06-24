using backend.Tests;

namespace backend.Tests.Integration;

[Collection("Integration")]
public class HealthEndpointTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task Health_ReturnsOk()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
    }
}
