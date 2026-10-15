namespace Inventory.Application.Auth;

public sealed record LoginCommand(string Email, string Password);

public sealed record IssuedToken(string AccessToken, DateTimeOffset ExpiresAt);

public sealed record LoginResult(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    string DisplayName,
    string Email,
    string Role);

public interface IAuthService
{
    Task<LoginResult> LoginAsync(LoginCommand command, CancellationToken cancellationToken);
}
