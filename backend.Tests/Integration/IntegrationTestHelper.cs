using System.Net.Http.Headers;
using System.Net.Http.Json;
using backend.Models;

namespace backend.Tests.Integration;

internal static class IntegrationTestHelper
{
    public static async Task<LoginResponse> LoginAsync( // ทดสอบ Login ด้วย Username และ Password
        HttpClient client,
        string username,
        string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest // ทดสอบ Login ด้วย Username และ Password
        {
            Username = username,
            Password = password,
        });

        response.EnsureSuccessStatusCode(); // ตรวจสอบว่า StatusCode เป็น 200 OK

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(); // อ่านข้อมูลจาก Login
        Assert.NotNull(body); // ตรวจสอบว่า Body ไม่เป็น null
        return body; // ส่งกลับข้อมูลจาก Login
    }

    public static async Task AuthenticateAsAsync( // ทดสอบ Authenticate ด้วย Username และ Password
        HttpClient client,
        string username,
        string password)
    {
        var body = await LoginAsync(client, username, password); // ทดสอบ Login ด้วย Username และ Password
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.AccessToken); // แนบ AccessToken ใน Header
    }
}
