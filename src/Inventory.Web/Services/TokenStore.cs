using System.Security.Cryptography;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace Inventory.Web.Services;

public sealed class TokenStore
{
    private const string Key = "northline.jwt";
    private readonly ProtectedSessionStorage _session;
    private string? _memory;

    public TokenStore(ProtectedSessionStorage session)
    {
        _session = session;
    }

    public async Task SetAsync(string token)
    {
        _memory = token;
        await _session.SetAsync(Key, token);
    }

    public async Task<string?> GetAsync()
    {
        if (_memory is not null)
        {
            return _memory;
        }

        try
        {
            var stored = await _session.GetAsync<string>(Key);
            _memory = stored.Success ? stored.Value : null;
            return _memory;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    public async Task ClearAsync()
    {
        _memory = null;
        try
        {
            await _session.DeleteAsync(Key);
        }
        catch (InvalidOperationException)
        {
        }
    }
}
