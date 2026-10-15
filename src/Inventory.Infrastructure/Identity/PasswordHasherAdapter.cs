using Inventory.Application.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace Inventory.Infrastructure.Identity;

public sealed class PasswordHasherAdapter : IPasswordHasher
{
    private static readonly object Marker = new();
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(Marker, password);

    public bool Verify(string passwordHash, string password)
    {
        var result = _hasher.VerifyHashedPassword(Marker, passwordHash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
