using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Inventory.Application.Auth;
using Inventory.Application.Dashboard;
using Inventory.Application.Products;
using Inventory.Application.Stock;
using Inventory.Domain.Products;

namespace Inventory.Api.Tests;

public class InventoryApiTests : IClassFixture<InventoryApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly InventoryApiFactory _factory;

    public InventoryApiTests(InventoryApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Products_require_a_token()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_rejects_a_wrong_password()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand("demo@northline.local", "nope"), Json);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Demo_user_can_create_receive_issue_and_count_stock()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var sku = $"T-{Guid.NewGuid():N}"[..12];

        var created = await PostAsync<ProductDto>(client, "/api/products", new CreateProductCommand(sku, "Test hinge", "bench", 2.5m, 4, 3));
        Assert.Equal(sku.ToUpperInvariant(), created.Sku);
        Assert.Equal(4, created.QuantityOnHand);
        Assert.False(created.IsLowStock);

        var movements = await GetAsync<List<StockMovementDto>>(client, $"/api/stock-movements?productId={created.Id}");
        Assert.Contains(movements, movement => movement.Note == "Opening balance" && movement.Delta == 4);

        var receipt = await PostAsync<StockMovementDto>(client, "/api/stock-movements", new RecordMovementCommand(created.Id, StockMovementType.Receipt, 2, "delivery"));
        Assert.Equal(6, receipt.QuantityAfter);

        var tooMany = await client.PostAsJsonAsync("/api/stock-movements", new RecordMovementCommand(created.Id, StockMovementType.Issue, 50, null), Json);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, tooMany.StatusCode);

        var issue = await PostAsync<StockMovementDto>(client, "/api/stock-movements", new RecordMovementCommand(created.Id, StockMovementType.Issue, 1, null));
        Assert.Equal(5, issue.QuantityAfter);

        var count = await PostAsync<StockMovementDto>(client, "/api/stock-movements", new RecordMovementCommand(created.Id, StockMovementType.Adjustment, 1, "cycle"));
        Assert.Equal(-4, count.Delta);
        Assert.True(count.QuantityAfter <= 3);

        var updated = await PutAsync<ProductDto>(client, $"/api/products/{created.Id}", new UpdateProductCommand("Test hinge renamed", "bench", 3m, 0, true));
        Assert.Equal("Test hinge renamed", updated.Name);
        Assert.Equal(1, updated.QuantityOnHand);

        var archived = await client.DeleteAsync($"/api/products/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, archived.StatusCode);
        var afterArchive = await GetAsync<ProductDto>(client, $"/api/products/{created.Id}");
        Assert.False(afterArchive.IsActive);

        var blocked = await client.PostAsJsonAsync("/api/stock-movements", new RecordMovementCommand(created.Id, StockMovementType.Receipt, 1, null), Json);
        Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);
    }

    [Fact]
    public async Task Duplicate_sku_and_invalid_input_are_rejected()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var duplicate = await client.PostAsJsonAsync("/api/products", new CreateProductCommand("nl-cop-12", "Copy", null, 1m, 0, 1), Json);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var invalid = await client.PostAsJsonAsync("/api/products", new CreateProductCommand("bad sku", "", null, -1m, 0, 1), Json);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        using var document = JsonDocument.Parse(await invalid.Content.ReadAsStringAsync());
        Assert.True(document.RootElement.TryGetProperty("errors", out var errors));
        Assert.Contains(errors.EnumerateObject(), property => property.Name.Equals("Sku", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Dashboard_includes_seeded_low_stock()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var dashboard = await GetAsync<DashboardDto>(client, "/api/dashboard");

        Assert.True(dashboard.ActiveProductCount >= 6);
        Assert.True(dashboard.LowStockCount >= 3);
        Assert.Contains(dashboard.LowStock, product => product.Sku == "NL-TAP-BL");
        Assert.True(dashboard.InventoryValue > 0);
    }

    [Fact]
    public async Task Swagger_and_health_are_public()
    {
        using var client = _factory.CreateClient();
        var swagger = await client.GetAsync("/swagger/v1/swagger.json");
        var health = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, swagger.StatusCode);
        Assert.Contains("Northline Inventory API", await swagger.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginCommand("demo@northline.local", "Demo1234!"),
            Json);
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResult>(Json);
        Assert.NotNull(login);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return client;
    }

    private static async Task<T> GetAsync<T>(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        return await ReadAsync<T>(response);
    }

    private static async Task<T> PostAsync<T>(HttpClient client, string url, object payload)
    {
        var response = await client.PostAsJsonAsync(url, payload, Json);
        return await ReadAsync<T>(response);
    }

    private static async Task<T> PutAsync<T>(HttpClient client, string url, object payload)
    {
        var response = await client.PutAsJsonAsync(url, payload, Json);
        return await ReadAsync<T>(response);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"{(int)response.StatusCode} {response.RequestMessage?.RequestUri}: {content}");
        }

        var body = await response.Content.ReadFromJsonAsync<T>(Json);
        return body ?? throw new InvalidOperationException("Empty response.");
    }
}
