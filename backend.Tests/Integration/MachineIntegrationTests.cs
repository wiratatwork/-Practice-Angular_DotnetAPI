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
    private static string NewMachineNo() => $"IT-{Guid.NewGuid():N}"[..10];

    private static CreateMachineDto NewMachineDto(string? machineNo = null, string? machineName = null) =>
        new()
        {
            MachineNo = machineNo ?? NewMachineNo(),
            MachineName = machineName ?? $"Machine-{Guid.NewGuid():N}"[..12],
            Plant = "P1",
            Status = "Active",
        };

    private static async Task<Machine> CreateMachineAsAdminAsync(HttpClient client, CreateMachineDto? dto = null)
    {
        dto ??= NewMachineDto();
        var response = await client.PostAsJsonAsync("/api/machine", dto);
        response.EnsureSuccessStatusCode();
        var machine = await response.Content.ReadFromJsonAsync<Machine>();
        Assert.NotNull(machine);
        return machine;
    }

    [Fact]
    public async Task GetAll_WithoutAuth_ReturnsUnauthorized()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/machine");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_AsUser_ReturnsOk()
    {
        using var client = factory.CreateClient();
        await IntegrationTestHelper.AuthenticateAsAsync(client, "user", "User@1234");

        var response = await client.GetAsync("/api/machine");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Create_AsUser_ReturnsForbidden()
    {
        using var client = factory.CreateClient();
        await IntegrationTestHelper.AuthenticateAsAsync(client, "user", "User@1234");

        var response = await client.PostAsJsonAsync("/api/machine", NewMachineDto());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_AsAdmin_ReturnsCreated()
    {
        using var client = factory.CreateClient();
        await IntegrationTestHelper.AuthenticateAsAsync(client, "admin", "Admin@1234");

        var dto = NewMachineDto();
        var response = await client.PostAsJsonAsync("/api/machine", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var machine = await response.Content.ReadFromJsonAsync<Machine>();
        Assert.NotNull(machine);
        Assert.Equal(dto.MachineNo, machine.MachineNo);
        Assert.Equal(dto.MachineName, machine.MachineName);
    }

    [Fact]
    public async Task GetByNo_AsUser_ReturnsMachine()
    {
        using var client = factory.CreateClient();
        await IntegrationTestHelper.AuthenticateAsAsync(client, "admin", "Admin@1234");

        var dto = NewMachineDto();
        await CreateMachineAsAdminAsync(client, dto);

        await IntegrationTestHelper.AuthenticateAsAsync(client, "user", "User@1234");
        var response = await client.GetAsync($"/api/machine/{dto.MachineNo}");

        response.EnsureSuccessStatusCode();

        var machine = await response.Content.ReadFromJsonAsync<Machine>();
        Assert.NotNull(machine);
        Assert.Equal(dto.MachineNo, machine.MachineNo);
    }

    [Fact]
    public async Task GetByNo_WhenNotFound_ReturnsNotFound()
    {
        using var client = factory.CreateClient();
        await IntegrationTestHelper.AuthenticateAsAsync(client, "user", "User@1234");

        var response = await client.GetAsync($"/api/machine/{NewMachineNo()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Search_AsUser_ReturnsMatches()
    {
        using var client = factory.CreateClient();
        await IntegrationTestHelper.AuthenticateAsAsync(client, "admin", "Admin@1234");

        var searchToken = $"Search{Guid.NewGuid():N}"[..16];
        var dto = NewMachineDto(machineName: searchToken);
        await CreateMachineAsAdminAsync(client, dto);

        await IntegrationTestHelper.AuthenticateAsAsync(client, "user", "User@1234");
        var response = await client.GetAsync($"/api/machine/search/{searchToken}");

        response.EnsureSuccessStatusCode();

        var machines = await response.Content.ReadFromJsonAsync<List<Machine>>();
        Assert.NotNull(machines);
        Assert.Contains(machines, m => m.MachineNo == dto.MachineNo);
    }

    [Fact]
    public async Task CheckDuplicateName_WhenExists_ReturnsTrue()
    {
        using var client = factory.CreateClient();
        await IntegrationTestHelper.AuthenticateAsAsync(client, "admin", "Admin@1234");

        var dto = NewMachineDto();
        await CreateMachineAsAdminAsync(client, dto);

        await IntegrationTestHelper.AuthenticateAsAsync(client, "user", "User@1234");
        var encodedName = Uri.EscapeDataString(dto.MachineName);
        var response = await client.GetAsync($"/api/machine/checkDuplicateName/{encodedName}");

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<DuplicateNameResponse>();
        Assert.NotNull(body);
        Assert.True(body.IsDuplicate);
    }

    [Fact]
    public async Task Update_AsAdmin_ReturnsOk()
    {
        using var client = factory.CreateClient();
        await IntegrationTestHelper.AuthenticateAsAsync(client, "admin", "Admin@1234");

        var dto = NewMachineDto();
        var created = await CreateMachineAsAdminAsync(client, dto);

        var updateDto = new UpdateMachineDto
        {
            MachineName = $"Updated-{Guid.NewGuid():N}"[..12],
            Plant = "P2",
            Status = "Idle",
        };

        var response = await client.PatchAsJsonAsync($"/api/machine/{created.MachineNo}", updateDto);

        response.EnsureSuccessStatusCode();

        var machine = await response.Content.ReadFromJsonAsync<Machine>();
        Assert.NotNull(machine);
        Assert.Equal(updateDto.MachineName, machine.MachineName);
        Assert.Equal(updateDto.Plant, machine.Plant);
        Assert.Equal(updateDto.Status, machine.Status);
    }

    [Fact]
    public async Task Delete_AsAdmin_ReturnsNoContent()
    {
        using var client = factory.CreateClient();
        await IntegrationTestHelper.AuthenticateAsAsync(client, "admin", "Admin@1234");

        var created = await CreateMachineAsAdminAsync(client);

        var response = await client.DeleteAsync($"/api/machine/{created.MachineNo}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await client.GetAsync($"/api/machine/{created.MachineNo}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Create_WithDuplicateMachineNo_ReturnsBadRequest()
    {
        using var client = factory.CreateClient();
        await IntegrationTestHelper.AuthenticateAsAsync(client, "admin", "Admin@1234");

        var dto = NewMachineDto();
        await CreateMachineAsAdminAsync(client, dto);

        var duplicateDto = NewMachineDto(machineNo: dto.MachineNo);
        var response = await client.PostAsJsonAsync("/api/machine", duplicateDto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        Assert.Equal("MACHINE_NO_DUPLICATE", document.RootElement.GetProperty("code").GetString());
    }

    private sealed class DuplicateNameResponse
    {
        public bool IsDuplicate { get; set; }
    }
}
