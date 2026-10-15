using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace Inventory.Web.Services;

public sealed class ApiAuthStateProvider : AuthenticationStateProvider
{
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());
    private readonly TokenStore _tokens;

    public ApiAuthStateProvider(TokenStore tokens)
    {
        _tokens = tokens;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _tokens.GetAsync();
        return new AuthenticationState(CreatePrincipal(token));
    }

    public async Task InitializeAsync()
    {
        var state = await GetAuthenticationStateAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(state));
    }

    public async Task SignInAsync(string token)
    {
        await _tokens.SetAsync(token);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(CreatePrincipal(token))));
    }

    public async Task SignOutAsync()
    {
        await _tokens.ClearAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(Anonymous)));
    }

    private static ClaimsPrincipal CreatePrincipal(string? token)
    {
        if (string.IsNullOrWhiteSpace(token) || !TryReadClaims(token, out var claims))
        {
            return Anonymous;
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "jwt", nameType: "name", roleType: "role");
        return new ClaimsPrincipal(identity);
    }

    private static bool TryReadClaims(string token, out List<Claim> claims)
    {
        claims = [];
        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(Decode(parts[1]));
            var root = document.RootElement;
            if (root.TryGetProperty("exp", out var exp) && exp.TryGetInt64(out var seconds))
            {
                if (DateTimeOffset.FromUnixTimeSeconds(seconds) <= DateTimeOffset.UtcNow)
                {
                    return false;
                }
            }

            Add(root, "sub", claims);
            Add(root, "email", claims);
            Add(root, "name", claims);
            Add(root, "role", claims);
            return claims.Count > 0;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static void Add(JsonElement root, string name, List<Claim> claims)
    {
        if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && value.GetString() is string text)
        {
            claims.Add(new Claim(name, text));
        }
    }

    private static string Decode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
        return Encoding.UTF8.GetString(Convert.FromBase64String(padded));
    }
}
