using FluentValidation;
using Inventory.Application.Abstractions;
using Inventory.Domain.Exceptions;

namespace Inventory.Application.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwords;
    private readonly IJwtTokenIssuer _tokens;
    private readonly TimeProvider _clock;
    private readonly IValidator<LoginCommand> _validator;

    public AuthService(
        IUserRepository users,
        IPasswordHasher passwords,
        IJwtTokenIssuer tokens,
        TimeProvider clock,
        IValidator<LoginCommand> validator)
    {
        _users = users;
        _passwords = passwords;
        _tokens = tokens;
        _clock = clock;
        _validator = validator;
    }

    public async Task<LoginResult> LoginAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(command, cancellationToken);
        var email = command.Email.Trim().ToLowerInvariant();
        var user = await _users.GetByEmailAsync(email, cancellationToken);
        if (user is null || !_passwords.Verify(user.PasswordHash, command.Password))
        {
            throw DomainException.Unauthorized("Email or password is incorrect.");
        }

        var issued = _tokens.Issue(user, _clock.GetUtcNow());
        return new LoginResult(issued.AccessToken, issued.ExpiresAt, user.DisplayName, user.Email, user.Role);
    }
}
