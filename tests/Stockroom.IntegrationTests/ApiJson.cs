using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Stockroom.Contracts.Dtos;
using Stockroom.Contracts.Requests;

namespace Stockroom.IntegrationTests;

internal static class ApiJson
{
    public static readonly JsonSerializerOptions Options = CreateOptions();

    public static async Task<HttpClient> AuthorizedClientAsync(StockroomApiFactory factory, string email = "admin@stockroom.demo", string password = "Demo-Admin-123!")
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        var login = JsonSerializer.Deserialize<LoginResponse>(body, Options);
        Assert.NotNull(login);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
        return client;
    }

    public static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        var payload = JsonSerializer.Deserialize<T>(body, Options);
        Assert.NotNull(payload);
        return payload!;
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
