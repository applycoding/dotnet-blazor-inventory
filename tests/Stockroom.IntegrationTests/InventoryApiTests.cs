using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Stockroom.Contracts.Dtos;
using Stockroom.Contracts.Requests;

namespace Stockroom.IntegrationTests;

public sealed class InventoryApiTests : IClassFixture<StockroomApiFactory>
{
    private readonly StockroomApiFactory _factory;

    public InventoryApiTests(StockroomApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_is_anonymous_and_uses_sqlite()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.Equal("ok", document.RootElement.GetProperty("status").GetString());
        Assert.Equal("Sqlite", document.RootElement.GetProperty("provider").GetString());
    }

    [Fact]
    public async Task Login_returns_a_token_for_the_seeded_admin()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin@stockroom.demo", "Demo-Admin-123!"));
        var login = await ApiJson.ReadAsync<LoginResponse>(response);

        Assert.False(string.IsNullOrWhiteSpace(login.AccessToken));
        Assert.Equal("Admin", login.Role);
        Assert.Equal("Demo Admin", login.DisplayName);
    }

    [Fact]
    public async Task Login_rejects_a_wrong_password()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin@stockroom.demo", "not-the-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Clerk_can_sign_in()
    {
        using var client = await ApiJson.AuthorizedClientAsync(_factory, "clerk@stockroom.demo", "Demo-Clerk-123!");

        var response = await client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Products_require_a_token()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_product_shows_up_in_search()
    {
        using var client = await ApiJson.AuthorizedClientAsync(_factory);
        var sku = NewSku();

        var createdResponse = await client.PostAsJsonAsync(
            "/api/products",
            new CreateProductRequest(sku, "Test kettle", "Notes", 12.5m, 5, 2));
        var created = await ApiJson.ReadAsync<ProductDto>(createdResponse);
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        Assert.Equal(sku, created.Sku);
        Assert.Equal(5, created.QuantityOnHand);

        var listedResponse = await client.GetAsync($"/api/products?search={sku}");
        var listed = await ApiJson.ReadAsync<PagedResult<ProductDto>>(listedResponse);
        Assert.Contains(listed.Items, product => product.Sku == sku);
    }

    [Fact]
    public async Task Duplicate_sku_is_a_conflict()
    {
        using var client = await ApiJson.AuthorizedClientAsync(_factory);
        var sku = NewSku();
        var request = new CreateProductRequest(sku, "Duplicate", null, 3m, 1, 0);

        var first = await client.PostAsJsonAsync("/api/products", request);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsJsonAsync("/api/products", request);
        var body = await second.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Contains("already exists", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Blank_sku_returns_validation_errors()
    {
        using var client = await ApiJson.AuthorizedClientAsync(_factory);

        var response = await client.PostAsJsonAsync(
            "/api/products",
            new CreateProductRequest(" ", "Kettle", null, 10m, 1, 1));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Sku", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Fulfill_commits_stock()
    {
        using var client = await ApiJson.AuthorizedClientAsync(_factory);
        var sku = NewSku();
        var product = await ApiJson.ReadAsync<ProductDto>(await client.PostAsJsonAsync(
            "/api/products",
            new CreateProductRequest(sku, "Fulfill kettle", null, 10m, 5, 1)));
        var email = $"buyer-{Guid.NewGuid():N}@example.com";
        var customer = await ApiJson.ReadAsync<CustomerDto>(await client.PostAsJsonAsync(
            "/api/customers",
            new CreateCustomerRequest("Fulfill Buyer", email, null)));

        var order = await ApiJson.ReadAsync<OrderDetailDto>(await client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest(customer.Id, [new CreateOrderLineRequest(product.Id, 2)], Submit: true)));
        Assert.Equal(20m, order.Total);

        var fulfilled = await ApiJson.ReadAsync<OrderDetailDto>(await client.PostAsync($"/api/orders/{order.Id}/fulfill", null));
        Assert.Equal("Fulfilled", fulfilled.Status.ToString());

        var after = await ApiJson.ReadAsync<ProductDto>(await client.GetAsync($"/api/products/{product.Id}"));
        Assert.Equal(3, after.QuantityOnHand);
    }

    [Fact]
    public async Task Fulfill_rejects_insufficient_stock_without_changing_it()
    {
        using var client = await ApiJson.AuthorizedClientAsync(_factory);
        var sku = NewSku();
        var product = await ApiJson.ReadAsync<ProductDto>(await client.PostAsJsonAsync(
            "/api/products",
            new CreateProductRequest(sku, "Scarce filters", null, 4m, 1, 0)));
        var customer = await ApiJson.ReadAsync<CustomerDto>(await client.PostAsJsonAsync(
            "/api/customers",
            new CreateCustomerRequest("Short Buyer", $"short-{Guid.NewGuid():N}@example.com", null)));
        var order = await ApiJson.ReadAsync<OrderDetailDto>(await client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest(customer.Id, [new CreateOrderLineRequest(product.Id, 2)], Submit: true)));

        var response = await client.PostAsync($"/api/orders/{order.Id}/fulfill", null);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("short of", body, StringComparison.OrdinalIgnoreCase);

        var after = await ApiJson.ReadAsync<ProductDto>(await client.GetAsync($"/api/products/{product.Id}"));
        Assert.Equal(1, after.QuantityOnHand);
    }

    [Fact]
    public async Task Dashboard_reports_seeded_low_stock()
    {
        using var client = await ApiJson.AuthorizedClientAsync(_factory);

        var dashboard = await ApiJson.ReadAsync<DashboardDto>(await client.GetAsync("/api/dashboard"));

        Assert.True(dashboard.LowStockProducts >= 1);
        Assert.Contains(dashboard.LowStock, product => product.Sku == "DRP-02");
    }

    private static string NewSku() => "T-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
}
