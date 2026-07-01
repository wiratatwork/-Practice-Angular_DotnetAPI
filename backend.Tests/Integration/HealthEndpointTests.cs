using backend.Tests;

namespace backend.Tests.Integration;

[Collection("Integration")]
public class HealthEndpointTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task Health_ReturnsOk() // ทดสอบ Health Endpoint ควรส่งกลับ 200 OK
    {
        using var client = factory.CreateClient(); // เรียกใช้ CustomWebApplicationFactory สร้าง HttpClient

        var response = await client.GetAsync("/health"); // ทดสอบ Health Endpoint

        response.EnsureSuccessStatusCode(); // ตรวจสอบว่า StatusCode เป็น 200 OK
    }
}
