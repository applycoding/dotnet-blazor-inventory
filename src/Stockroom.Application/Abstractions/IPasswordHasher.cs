using Stockroom.Domain.Entities;

namespace Stockroom.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(User user, string password);

    bool Verify(User? user, string password);
}
