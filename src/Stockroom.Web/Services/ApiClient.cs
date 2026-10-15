using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using Stockroom.Contracts.Dtos;
using Stockroom.Contracts.Requests;
using Stockroom.Domain.Enums;

namespace Stockroom.Web.Services;

public sealed class ApiClient
{
    private static readonly JsonSerializerOptions Json = CreateOptions();

    private readonly HttpClient _http;
    private readonly AuthState _auth;
    private readonly IJSRuntime _js;
    private readonly NavigationManager _navigation;

    public ApiClient(HttpClient http, AuthState auth, IJSRuntime js, NavigationManager navigation)
    {
        _http = http;
        _auth = auth;
        _js = js;
        _navigation = navigation;
    }

    public Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<LoginResponse>(HttpMethod.Post, "api/auth/login", request, cancellationToken);

    public Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default) =>
        SendAsync<DashboardDto>(HttpMethod.Get, "api/dashboard", null, cancellationToken);

    public Task<PagedResult<ProductDto>> GetProductsAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = $"api/products?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
        {
            query += $"&search={Uri.EscapeDataString(search.Trim())}";
        }

        return SendAsync<PagedResult<ProductDto>>(HttpMethod.Get, query, null, cancellationToken);
    }

    public Task<ProductDto> GetProductAsync(Guid id, CancellationToken cancellationToken = default) =>
        SendAsync<ProductDto>(HttpMethod.Get, $"api/products/{id}", null, cancellationToken);

    public Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<ProductDto>(HttpMethod.Post, "api/products", request, cancellationToken);

    public Task<ProductDto> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<ProductDto>(HttpMethod.Put, $"api/products/{id}", request, cancellationToken);

    public Task<ProductDto> AdjustStockAsync(Guid id, AdjustStockRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<ProductDto>(HttpMethod.Post, $"api/products/{id}/stock-adjustments", request, cancellationToken);

    public Task<IReadOnlyList<CustomerDto>> GetCustomersAsync(string? search, CancellationToken cancellationToken = default)
    {
        var query = "api/customers";
        if (!string.IsNullOrWhiteSpace(search))
        {
            query += $"?search={Uri.EscapeDataString(search.Trim())}";
        }

        return SendAsync<IReadOnlyList<CustomerDto>>(HttpMethod.Get, query, null, cancellationToken);
    }

    public Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<CustomerDto>(HttpMethod.Post, "api/customers", request, cancellationToken);

    public Task<PagedResult<OrderSummaryDto>> GetOrdersAsync(OrderStatus? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = $"api/orders?page={page}&pageSize={pageSize}";
        if (status is not null)
        {
            query += $"&status={status}";
        }

        return SendAsync<PagedResult<OrderSummaryDto>>(HttpMethod.Get, query, null, cancellationToken);
    }

    public Task<OrderDetailDto> GetOrderAsync(Guid id, CancellationToken cancellationToken = default) =>
        SendAsync<OrderDetailDto>(HttpMethod.Get, $"api/orders/{id}", null, cancellationToken);

    public Task<OrderDetailDto> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<OrderDetailDto>(HttpMethod.Post, "api/orders", request, cancellationToken);

    public Task<OrderDetailDto> SubmitOrderAsync(Guid id, CancellationToken cancellationToken = default) =>
        SendAsync<OrderDetailDto>(HttpMethod.Post, $"api/orders/{id}/submit", null, cancellationToken);

    public Task<OrderDetailDto> CancelOrderAsync(Guid id, CancellationToken cancellationToken = default) =>
        SendAsync<OrderDetailDto>(HttpMethod.Post, $"api/orders/{id}/cancel", null, cancellationToken);

    public Task<OrderDetailDto> FulfillOrderAsync(Guid id, CancellationToken cancellationToken = default) =>
        SendAsync<OrderDetailDto>(HttpMethod.Post, $"api/orders/{id}/fulfill", null, cancellationToken);

    private async Task<T> SendAsync<T>(HttpMethod method, string uri, object? body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, uri);
        if (!string.IsNullOrWhiteSpace(_auth.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.Token);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: Json);
        }

        using var response = await _http.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized && !uri.EndsWith("/api/auth/login", StringComparison.OrdinalIgnoreCase))
        {
            await _auth.SignOutAsync(_js);
            _navigation.NavigateTo("/login");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException((int)response.StatusCode, await ReadProblemAsync(response, cancellationToken));
        }

        var payload = await response.Content.ReadFromJsonAsync<T>(Json, cancellationToken);
        if (payload is null)
        {
            throw new ApiException((int)response.StatusCode, "The server returned an empty response.");
        }

        return payload;
    }

    private static async Task<string> ReadProblemAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(body))
        {
            return $"Request failed ({(int)response.StatusCode}).";
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                var messages = new List<string>();
                foreach (var property in errors.EnumerateObject())
                {
                    if (property.Value.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (var item in property.Value.EnumerateArray())
                    {
                        var message = item.GetString();
                        if (!string.IsNullOrWhiteSpace(message))
                        {
                            messages.Add(message);
                        }
                    }
                }

                if (messages.Count > 0)
                {
                    return string.Join(" ", messages);
                }
            }

            if (root.TryGetProperty("detail", out var detail))
            {
                var message = detail.GetString();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    return message;
                }
            }
        }
        catch (JsonException)
        {
            return body;
        }

        return $"Request failed ({(int)response.StatusCode}).";
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
