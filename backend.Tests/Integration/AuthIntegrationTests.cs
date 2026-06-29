using System.Net; // HttpStatusCode
using System.Net.Http.Json;
using backend.Models; // LoginRequest, RefreshResponse
using backend.Tests; // IntegrationTestHelper

namespace backend.Tests.Integration;

[Collection("Integration")] // IntegrationTestCollection
public class AuthIntegrationTests(CustomWebApplicationFactory factory)
{
    [Fact] // เพื่อให้อยู่ในกลุ่ม IntegrationTestCollection
    public async Task Login_WithSeededAdmin_ReturnsAccessToken() // ทดสอบ Login ด้วย User 'admin' ที่ Seed
    {
        using var client = factory.CreateClient(); // เรียกใช้ CustomWebApplicationFactory สร้าง HttpClient

        var body = await IntegrationTestHelper.LoginAsync(client, "admin", "Admin@1234"); // ทดสอบ Login ด้วย User 'admin' ที่ Seed

        Assert.False(string.IsNullOrWhiteSpace(body.AccessToken)); // ตรวจสอบว่า AccessToken ไม่เป็นค่าว่าง
        Assert.Equal("admin", body.User.Username); // ตรวจสอบว่า Username เป็น 'admin'
        Assert.Equal("Admin", body.User.Role); // ตรวจสอบว่า Role เป็น 'Admin'
    }

    [Fact]
    public async Task Login_WithSeededUser_ReturnsAccessToken() // ทดสอบ Login ด้วย User 'user' ที่ Seed
    {
        using var client = factory.CreateClient(); // เรียกใช้ CustomWebApplicationFactory สร้าง HttpClient

        var body = await IntegrationTestHelper.LoginAsync(client, "user", "User@1234"); // ทดสอบ Login ด้วย User 'user' ที่ Seed

        Assert.False(string.IsNullOrWhiteSpace(body.AccessToken)); // ตรวจสอบว่า AccessToken ไม่เป็นค่าว่าง
        Assert.Equal("user", body.User.Username); // ตรวจสอบว่า Username เป็น 'user'
        Assert.Equal("User", body.User.Role); // ตรวจสอบว่า Role เป็น 'User'
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized() // ทดสอบ Login ด้วย Password ผิด
    {
        using var client = factory.CreateClient(); // เรียกใช้ CustomWebApplicationFactory สร้าง HttpClient

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest // ทดสอบ Login ด้วย Password ผิด
        {
            Username = "admin",
            Password = "WrongPassword",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode); // ตรวจสอบว่า StatusCode เป็น 401 Unauthorized
    }

    [Fact]
    public async Task Login_WithEmptyUsername_ReturnsBadRequest() // ทดสอบ Login ด้วย Username ว่าง
    {
        using var client = factory.CreateClient(); // เรียกใช้ CustomWebApplicationFactory สร้าง HttpClient

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest // ทดสอบ Login ด้วย Username ว่าง
        {
            Username = "",
            Password = "Admin@1234",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); // ตรวจสอบว่า StatusCode เป็น 400 BadRequest
    }

    [Fact]
    public async Task Refresh_AfterLogin_ReturnsNewAccessToken() // ทดสอบ Refresh หลังจาก Login
    {
        using var client = factory.CreateClient(); // เรียกใช้ CustomWebApplicationFactory สร้าง HttpClient

        var loginBody = await IntegrationTestHelper.LoginAsync(client, "admin", "Admin@1234"); // ทดสอบ Login ด้วย User 'admin' ที่ Seed

        var response = await client.PostAsync("/api/auth/refresh", null); // ทดสอบ Refresh

        response.EnsureSuccessStatusCode(); // ตรวจสอบว่า StatusCode เป็น 200 OK

        var refreshBody = await response.Content.ReadFromJsonAsync<RefreshResponse>(); // อ่านข้อมูลจาก Refresh
        Assert.NotNull(refreshBody); // ตรวจสอบว่า RefreshBody ไม่เป็น null
        Assert.False(string.IsNullOrWhiteSpace(refreshBody.AccessToken)); // ตรวจสอบว่า AccessToken ไม่เป็นค่าว่าง
        Assert.NotEqual(loginBody.AccessToken, refreshBody.AccessToken); // ตรวจสอบว่า AccessToken ไม่เท่ากับ LoginBody.AccessToken
    }

    [Fact]
    public async Task Refresh_WithoutCookie_ReturnsUnauthorized() // ทดสอบ Refresh โดยไม่มี Cookie
    {
        using var client = factory.CreateClient(); // เรียกใช้ CustomWebApplicationFactory สร้าง HttpClient

        var response = await client.PostAsync("/api/auth/refresh", null); // ทดสอบ Refresh โดยไม่มี Cookie

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode); // ตรวจสอบว่า StatusCode เป็น 401 Unauthorized
    }

    [Fact]
    public async Task Logout_AfterLogin_ReturnsNoContent() // ทดสอบ Logout หลังจาก Login
    {
        using var client = factory.CreateClient(); // เรียกใช้ CustomWebApplicationFactory สร้าง HttpClient

        await IntegrationTestHelper.LoginAsync(client, "admin", "Admin@1234"); // ทดสอบ Login ด้วย User 'admin' ที่ Seed

        var response = await client.PostAsync("/api/auth/logout", null); // ทดสอบ Logout

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode); // ตรวจสอบว่า StatusCode เป็น 204 NoContent
    }

    [Fact]
    public async Task Logout_WithoutCookie_ReturnsNoContent() // ทดสอบ Logout โดยไม่มี Cookie
    {
        using var client = factory.CreateClient(); // เรียกใช้ CustomWebApplicationFactory สร้าง HttpClient

        var response = await client.PostAsync("/api/auth/logout", null); // ทดสอบ Logout โดยไม่มี Cookie  

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode); // ตรวจสอบว่า StatusCode เป็น 204 NoContent
    }
}
