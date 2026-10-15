using System.Net.Http.Headers;
using System.Net.Http.Json;
using Inventory.Web.Models;

namespace Inventory.Web.Services;

public sealed class InventoryApiClient
{
    private readonly HttpClient _http;
    private readonly TokenStore _tokens;

    public InventoryApiClient(HttpClient http, TokenStore tokens)
    {
        _http = http;
        _tokens = tokens;
    }

    public Task<LoginResultModel> LoginAsync(string email, string password, CancellationToken cancellationToken = default) =>
        SendAsync<LoginResultModel>(
            () => _http.PostAsJsonAsync("api/auth/login", new { email, password }, ApiJson.Options, cancellationToken),
            cancellationToken);

    public Task<PagedProducts> GetProductsAsync(
        string? search,
        bool includeArchived,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}",
            $"includeArchived={includeArchived.ToString().ToLowerInvariant()}"
        };
        if (!string.IsNullOrWhiteSpace(search))
        {
            query.Add($"search={Uri.EscapeDataString(search.Trim())}");
        }

        return SendAsync<PagedProducts>(
            () => _http.GetAsync("api/products?" + string.Join('&', query), cancellationToken),
            cancellationToken);
    }

    public Task<ProductModel> GetProductAsync(Guid id, CancellationToken cancellationToken = default) =>
        SendAsync<ProductModel>(() => _http.GetAsync($"api/products/{id}", cancellationToken), cancellationToken);

    public Task<ProductModel> CreateProductAsync(ProductDraft draft, CancellationToken cancellationToken = default) =>
        SendAsync<ProductModel>(
            () => _http.PostAsJsonAsync(
                "api/products",
                new
                {
                    sku = draft.Sku,
                    name = draft.Name,
                    description = draft.Description,
                    unitPrice = draft.UnitPrice,
                    initialQuantity = draft.InitialQuantity,
                    reorderLevel = draft.ReorderLevel
                },
                ApiJson.Options,
                cancellationToken),
            cancellationToken);

    public Task<ProductModel> UpdateProductAsync(Guid id, ProductDraft draft, CancellationToken cancellationToken = default) =>
        SendAsync<ProductModel>(
            () => _http.PutAsJsonAsync(
                $"api/products/{id}",
                new
                {
                    name = draft.Name,
                    description = draft.Description,
                    unitPrice = draft.UnitPrice,
                    reorderLevel = draft.ReorderLevel,
                    isActive = draft.IsActive
                },
                ApiJson.Options,
                cancellationToken),
            cancellationToken);

    public async Task ArchiveProductAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await AttachTokenAsync();
        using var response = await _http.DeleteAsync($"api/products/{id}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw await ApiException.FromAsync(response, cancellationToken);
        }
    }

    public Task<IReadOnlyList<MovementModel>> GetMovementsAsync(Guid? productId, CancellationToken cancellationToken = default)
    {
        var path = productId is Guid id
            ? $"api/stock-movements?productId={id}&take=40"
            : "api/stock-movements?take=40";
        return SendAsync<IReadOnlyList<MovementModel>>(() => _http.GetAsync(path, cancellationToken), cancellationToken);
    }

    public Task<MovementModel> RecordMovementAsync(MovementDraft draft, CancellationToken cancellationToken = default) =>
        SendAsync<MovementModel>(
            () => _http.PostAsJsonAsync(
                "api/stock-movements",
                new
                {
                    productId = draft.ProductId,
                    type = draft.Type,
                    quantity = draft.Quantity,
                    note = draft.Note
                },
                ApiJson.Options,
                cancellationToken),
            cancellationToken);

    public Task<DashboardModel> GetDashboardAsync(CancellationToken cancellationToken = default) =>
        SendAsync<DashboardModel>(() => _http.GetAsync("api/dashboard", cancellationToken), cancellationToken);

    private async Task<T> SendAsync<T>(Func<Task<HttpResponseMessage>> send, CancellationToken cancellationToken)
    {
        await AttachTokenAsync();
        using var response = await send();
        if (!response.IsSuccessStatusCode)
        {
            throw await ApiException.FromAsync(response, cancellationToken);
        }

        var body = await response.Content.ReadFromJsonAsync<T>(ApiJson.Options, cancellationToken);
        return body ?? throw new ApiException((int)response.StatusCode, "The API returned an empty response.", new Dictionary<string, string[]>());
    }

    private async Task AttachTokenAsync()
    {
        var token = await _tokens.GetAsync();
        _http.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(token)
            ? null
            : new AuthenticationHeaderValue("Bearer", token);
    }
}
