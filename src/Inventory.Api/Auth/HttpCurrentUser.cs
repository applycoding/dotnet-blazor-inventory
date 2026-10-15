using System.IdentityModel.Tokens.Jwt;
using Inventory.Application.Abstractions;

namespace Inventory.Api.Auth;

public sealed class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string Email =>
        _httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Email)?.Value
        ?? "unknown";

    public string DisplayName =>
        _httpContextAccessor.HttpContext?.User.FindFirst("name")?.Value
        ?? Email;
}
