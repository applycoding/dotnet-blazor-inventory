using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

using Stockroom.Application.Abstractions;
using Stockroom.Application.Options;
using Stockroom.Domain.Entities;
using Stockroom.Domain.Enums;

namespace Stockroom.Infrastructure.Identity;

public sealed class IdentityPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();
    private readonly User _placeholder;
    private readonly string _dummyHash;

    public IdentityPasswordHasher()
    {
        _placeholder = User.Create("nobody@stockroom.demo", "Nobody", UserRole.Clerk, "placeholder");
        _dummyHash = _hasher.HashPassword(_placeholder, "dummy-password-not-used");
    }

    public string Hash(User user, string password)
    {
        return _hasher.HashPassword(user, password);
    }

    public bool Verify(User? user, string password)
    {
        var subject = user ?? _placeholder;
        var hash = user?.PasswordHash ?? _dummyHash;
        var result = _hasher.VerifyHashedPassword(subject, hash, password);
        return user is not null && result != PasswordVerificationResult.Failed;
    }
}

public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtOptions _options;

    public JwtTokenGenerator(JwtOptions options)
    {
        _options = options;
    }

    public (string Token, DateTimeOffset ExpiresAtUtc) CreateToken(User user)
    {
        var expires = DateTimeOffset.UtcNow.AddMinutes(_options.ExpiryMinutes);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)),
            SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Role, user.Role.ToString())
        };
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
