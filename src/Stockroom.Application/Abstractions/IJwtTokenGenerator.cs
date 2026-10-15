using Stockroom.Domain.Entities;

namespace Stockroom.Application.Abstractions;

public interface IJwtTokenGenerator
{
    (string Token, DateTimeOffset ExpiresAtUtc) CreateToken(User user);
}
