using System.Text.Json;

using Microsoft.JSInterop;

using Stockroom.Contracts.Dtos;

namespace Stockroom.Web.Services;

public sealed class AuthState
{
    public const string StorageKey = "stockroom.session";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public bool IsReady { get; private set; }

    public string? Token { get; private set; }

    public string? Email { get; private set; }

    public string? DisplayName { get; private set; }

    public string? Role { get; private set; }

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Token);

    public async Task InitializeAsync(IJSRuntime js)
    {
        if (IsReady)
        {
            return;
        }

        try
        {
            var stored = await js.InvokeAsync<string?>("stockroomStorage.get", StorageKey);
            if (!string.IsNullOrWhiteSpace(stored))
            {
                var session = JsonSerializer.Deserialize<StoredSession>(stored, Json);
                if (session is not null
                    && !string.IsNullOrWhiteSpace(session.Token)
                    && session.ExpiresAtUtc > DateTimeOffset.UtcNow)
                {
                    Apply(session);
                }
                else
                {
                    await js.InvokeVoidAsync("stockroomStorage.remove", StorageKey);
                }
            }
        }
        catch (JSException)
        {
            Token = null;
        }

        IsReady = true;
    }

    public async Task SignInAsync(IJSRuntime js, LoginResponse response)
    {
        var session = new StoredSession(
            response.AccessToken,
            response.Email,
            response.DisplayName,
            response.Role,
            response.ExpiresAtUtc);
        Apply(session);
        await js.InvokeVoidAsync("stockroomStorage.set", StorageKey, JsonSerializer.Serialize(session, Json));
    }

    public async Task SignOutAsync(IJSRuntime js)
    {
        Token = null;
        Email = null;
        DisplayName = null;
        Role = null;
        try
        {
            await js.InvokeVoidAsync("stockroomStorage.remove", StorageKey);
        }
        catch (JSException)
        {
            // The session is already cleared in memory.
        }
    }

    private void Apply(StoredSession session)
    {
        Token = session.Token;
        Email = session.Email;
        DisplayName = session.DisplayName;
        Role = session.Role;
    }

    private sealed record StoredSession(
        string Token,
        string Email,
        string DisplayName,
        string Role,
        DateTimeOffset ExpiresAtUtc);
}
