using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using backend.Controllers;
using backend.Models;
using backend.Tests;

namespace backend.Tests.Integration;

[Collection("Integration")]
public class MachineIntegrationTests(CustomWebApplicationFactory factory)
{
    private static string NewMachineNo() => $"IT-{Guid.NewGuid():N}"[..10]; // สร้าง MachineNo สุ่ม

    private static CreateMachineDto NewMachineDto(string? machineNo = null, string? machineName = null) => // สร้าง CreateMachineDto สุ่ม
        new()
        {
            MachineNo = machineNo ?? NewMachineNo(),
            MachineName = machineName ?? $"Machine-{Guid.NewGuid():N}"[..12],
            Plant = "P1",
            Status = "Active",
        };

    private static async Task<Machine> CreateMachineAsAdminAsync(HttpClient client, CreateMachineDto? dto = null) // สร้าง Machine เป็น Admin
    {
        dto ??= NewMachineDto(); // ถ้า dto เป็น null ก็สร้าง CreateMachineDto สุ่ม
        var response = await client.PostAsJsonAsync("/api/machine", dto); // สร้าง Machine
        response.EnsureSuccessStatusCode(); // ตรวจสอบว่า StatusCode เป็น 200 OK
        var machine = await response.Content.ReadFromJsonAsync<Machine>(); // อ่านข้อมูลจาก Machine
        Assert.NotNull(machine); // ตรวจสอบว่า Machine ไม่เป็น null
        return machine; // ส่งกลับข้อมูลจาก Machine
    }

    [Fact]
    public async Task GetAll_WithoutAuth_ReturnsUnauthorized() // ทดสอบ GetAll โดยไม่มี Authentication
    {
        using var client = factory.CreateClient(); // เรียกใช้ CustomWebApplicationFactory สร้าง HttpClient

        var response = await client.GetAsync("/api/machine"); // ทดสอบ GetAll

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode); // ตรวจสอบว่า StatusCode เป็น 401 Unauthorized
    }

    [Fact]
    public async Task GetAll_AsUser_ReturnsOk() // ทดสอบ GetAll เป็น User
    {
        using var client = factory.CreateClient(); // เรียกใช้ CustomWebApplicationFactory สร้าง HttpClient
        await IntegrationTestHelper.AuthenticateAsAsync(client, "user", "User@1234"); // ทดสอบ Authenticate ด้วย User 'user' ที่ Seed

        var response = await client.GetAsync("/api/machine"); // ทดสอบ GetAll

        response.EnsureSuccessStatusCode(); // ตรวจสอบว่า StatusCode เป็น 200 OK
    }

    [Fact]
    public async Task Create_AsUser_ReturnsForbidden() // ทดสอบ Create โดยไม่มี Permission
    {
        using var client = factory.CreateClient(); // เรียกใช้ CustomWebApplicationFactory สร้าง HttpClient
        await IntegrationTestHelper.AuthenticateAsAsync(client, "user", "User@1234"); // ทดสอบ Authenticate ด้วย User 'user' ที่ Seed

        var response = await client.PostAsJsonAsync("/api/machine", NewMachineDto()); // ทดสอบ Create

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode); // ตรวจสอบว่า StatusCode เป็น 403 Forbidden
    }

    [Fact]
    public async Task Create_AsAdmin_ReturnsCreated() // ทดสอบ Create เป็น Admin
    {
        using var client = factory.CreateClient(); // เรียกใช้ CustomWebApplicationFactory สร้าง HttpClient
        await IntegrationTestHelper.AuthenticateAsAsync(client, "admin", "Admin@1234"); // ทดสอบ Authenticate ด้วย User 'admin' ที่ Seed

        var dto = NewMachineDto();
        var response = await client.PostAsJsonAsync("/api/machine", dto); // ทดสอบ Create

        Assert.Equal(HttpStatusCode.Created, response.StatusCode); // ตรวจสอบว่า StatusCode เป็น 201 Created

        var machine = await response.Content.ReadFromJsonAsync<Machine>();
        Assert.NotNull(machine); // ตรวจสอบว่า Machine ไม่เป็น null
        Assert.Equal(dto.MachineNo, machine.MachineNo); // ตรวจสอบว่า MachineNo เป็นตรงกับ dto.MachineNo
        Assert.Equal(dto.MachineName, machine.MachineName); // ตรวจสอบว่า MachineName เป็นตรงกับ dto.MachineName
    }

    [Fact]
    public async Task GetByNo_AsUser_ReturnsMachine() // ทดสอบ GetByNo เป็น User
    {
        using var client = factory.CreateClient(); // เรียกใช้ CustomWebApplicationFactory สร้าง HttpClient
        await IntegrationTestHelper.AuthenticateAsAsync(client, "admin", "Admin@1234"); // ทดสอบ Authenticate ด้วย User 'admin' ที่ Seed

        var dto = NewMachineDto();
        await CreateMachineAsAdminAsync(client, dto); // ทดสอบ Create Machine เป็น Admin

        await IntegrationTestHelper.AuthenticateAsAsync(client, "user", "User@1234"); // ทดสอบ Authenticate ด้วย User 'user' ที่ Seed
        var response = await client.GetAsync($"/api/machine/{dto.MachineNo}"); // ทดสอบ GetByNo

        response.EnsureSuccessStatusCode(); // ตรวจสอบว่า StatusCode เป็น 200 OK

        var machine = await response.Content.ReadFromJsonAsync<Machine>();
        Assert.NotNull(machine); // ตรวจสอบว่า Machine ไม่เป็น null
        Assert.Equal(dto.MachineNo, machine.MachineNo); // ตรวจสอบว่า MachineNo เป็นตรงกับ dto.MachineNo
    }

    [Fact]
    public async Task GetByNo_WhenNotFound_ReturnsNotFound() // ทดสอบ GetByNo โดยไม่พบ Machine
    {
        using var client = factory.CreateClient(); // เรียกใช้ CustomWebApplicationFactory สร้าง HttpClient
        await IntegrationTestHelper.AuthenticateAsAsync(client, "user", "User@1234"); // ทดสอบ Authenticate ด้วย User 'user' ที่ Seed

        var response = await client.GetAsync($"/api/machine/{NewMachineNo()}"); // ทดสอบ GetByNo

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode); // ตรวจสอบว่า StatusCode เป็น 404 NotFound
    }

    [Fact]
    public async Task Search_AsUser_ReturnsMatches() // ทดสอบ Search เป็น User
    {
        using var client = factory.CreateClient(); // เรียกใช้ CustomWebApplicationFactory สร้าง HttpClient
        await IntegrationTestHelper.AuthenticateAsAsync(client, "admin", "Admin@1234"); // ทดสอบ Authenticate ด้วย User 'admin' ที่ Seed

        var searchToken = $"Search{Guid.NewGuid():N}"[..16]; // สร้าง SearchToken สุ่ม
        var dto = NewMachineDto(machineName: searchToken);
        await CreateMachineAsAdminAsync(client, dto); // ทดสอบ Create Machine เป็น Admin

        await IntegrationTestHelper.AuthenticateAsAsync(client, "user", "User@1234"); // ทดสอบ Authenticate ด้วย User 'user' ที่ Seed
        var response = await client.GetAsync($"/api/machine/search/{searchToken}"); // ทดสอบ Search

        response.EnsureSuccessStatusCode(); // ตรวจสอบว่า StatusCode เป็น 200 OK

        var machines = await response.Content.ReadFromJsonAsync<List<Machine>>();
        Assert.NotNull(machines); // ตรวจสอบว่า Machines ไม่เป็น null
        Assert.Contains(machines, m => m.MachineNo == dto.MachineNo); // ตรวจสอบว่า MachineNo เป็นตรงกับ dto.MachineNo
    }

    [Fact]
    public async Task CheckDuplicateName_WhenExists_ReturnsTrue() // ทดสอบ CheckDuplicateName โดยมี Machine ซ้ำ
    {
        using var client = factory.CreateClient(); // เรียกใช้ CustomWebApplicationFactory สร้าง HttpClient
        await IntegrationTestHelper.AuthenticateAsAsync(client, "admin", "Admin@1234"); // ทดสอบ Authenticate ด้วย User 'admin' ที่ Seed

        var dto = NewMachineDto();
        await CreateMachineAsAdminAsync(client, dto); // ทดสอบ Create Machine เป็น Admin 

        await IntegrationTestHelper.AuthenticateAsAsync(client, "user", "User@1234"); // ทดสอบ Authenticate ด้วย User 'user' ที่ Seed
        var encodedName = Uri.EscapeDataString(dto.MachineName); // สร้าง encodedName จาก MachineName
        var response = await client.GetAsync($"/api/machine/checkDuplicateName/{encodedName}"); // ทดสอบ CheckDuplicateName

        response.EnsureSuccessStatusCode(); // ตรวจสอบว่า StatusCode เป็น 200 OK

        var body = await response.Content.ReadFromJsonAsync<DuplicateNameResponse>();
        Assert.NotNull(body); // ตรวจสอบว่า Body ไม่เป็น null
        Assert.True(body.IsDuplicate); // ตรวจสอบว่า IsDuplicate เป็น true
    }

    [Fact]
    public async Task Update_AsAdmin_ReturnsOk() // ทดสอบ Update เป็น Admin
    {
        using var client = factory.CreateClient(); // เรียกใช้ CustomWebApplicationFactory สร้าง HttpClient
        await IntegrationTestHelper.AuthenticateAsAsync(client, "admin", "Admin@1234"); // ทดสอบ Authenticate ด้วย User 'admin' ที่ Seed

        var dto = NewMachineDto();
        var created = await CreateMachineAsAdminAsync(client, dto); // ทดสอบ Create Machine เป็น Admin

        var updateDto = new UpdateMachineDto // สร้าง UpdateMachineDto
        {
            MachineName = $"Updated-{Guid.NewGuid():N}"[..12], // สร้าง MachineName สุ่ม
            Plant = "P2",
            Status = "Idle",
        };

        var response = await client.PatchAsJsonAsync($"/api/machine/{created.MachineNo}", updateDto); // ทดสอบ Update

        response.EnsureSuccessStatusCode(); // ตรวจสอบว่า StatusCode เป็น 200 OK

        var machine = await response.Content.ReadFromJsonAsync<Machine>();
        Assert.NotNull(machine); // ตรวจสอบว่า Machine ไม่เป็น null
        Assert.Equal(updateDto.MachineName, machine.MachineName); // ตรวจสอบว่า MachineName เป็นตรงกับ updateDto.MachineName
        Assert.Equal(updateDto.Plant, machine.Plant); // ตรวจสอบว่า Plant เป็นตรงกับ updateDto.Plant
        Assert.Equal(updateDto.Status, machine.Status); // ตรวจสอบว่า Status เป็นตรงกับ updateDto.Status
    }

    [Fact]
    public async Task Delete_AsAdmin_ReturnsNoContent() // ทดสอบ Delete เป็น Admin
    {
        using var client = factory.CreateClient(); // เรียกใช้ CustomWebApplicationFactory สร้าง HttpClient
        await IntegrationTestHelper.AuthenticateAsAsync(client, "admin", "Admin@1234"); // ทดสอบ Authenticate ด้วย User 'admin' ที่ Seed

        var created = await CreateMachineAsAdminAsync(client); // ทดสอบ Create Machine เป็น Admin

        var response = await client.DeleteAsync($"/api/machine/{created.MachineNo}"); // ทดสอบ Delete

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode); // ตรวจสอบว่า StatusCode เป็น 204 NoContent

        var getResponse = await client.GetAsync($"/api/machine/{created.MachineNo}"); // ทดสอบ GetByNo
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode); // ตรวจสอบว่า StatusCode เป็น 404 NotFound
    }

    [Fact]
    public async Task Create_WithDuplicateMachineNo_ReturnsBadRequest() // ทดสอบ Create โดยมี MachineNo ซ้ำ
    {
        using var client = factory.CreateClient(); // เรียกใช้ CustomWebApplicationFactory สร้าง HttpClient
        await IntegrationTestHelper.AuthenticateAsAsync(client, "admin", "Admin@1234"); // ทดสอบ Authenticate ด้วย User 'admin' ที่ Seed

        var dto = NewMachineDto();
        await CreateMachineAsAdminAsync(client, dto); // ทดสอบ Create Machine เป็น Admin

        var duplicateDto = NewMachineDto(machineNo: dto.MachineNo);
        var response = await client.PostAsJsonAsync("/api/machine", duplicateDto); // ทดสอบ Create ด้วย MachineNo ซ้ำ

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); // ตรวจสอบว่า StatusCode เป็น 400 BadRequest

        var json = await response.Content.ReadAsStringAsync(); // อ่านข้อมูลจาก response
        using var document = JsonDocument.Parse(json); // อ่านข้อมูลจาก document
        Assert.Equal("MACHINE_NO_DUPLICATE", document.RootElement.GetProperty("code").GetString()); // ตรวจสอบว่า code เป็น "MACHINE_NO_DUPLICATE"
    }

    private sealed class DuplicateNameResponse
    {
        public bool IsDuplicate { get; set; }
    }
}
