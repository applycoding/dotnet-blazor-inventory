namespace Inventory.Domain.Identity;

public sealed class AppUser
{
    public const int EmailMaxLength = 200;
    public const int DisplayNameMaxLength = 120;
    public const int RoleMaxLength = 40;

    private AppUser()
    {
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public string Role { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public static AppUser Create(
        string email,
        string displayName,
        string passwordHash,
        string role,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Trim().Length > EmailMaxLength)
        {
            throw new ArgumentException("A valid email is required.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(displayName) || displayName.Trim().Length > DisplayNameMaxLength)
        {
            throw new ArgumentException("A display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("A password hash is required.", nameof(passwordHash));
        }

        if (string.IsNullOrWhiteSpace(role) || role.Trim().Length > RoleMaxLength)
        {
            throw new ArgumentException("A role is required.", nameof(role));
        }

        return new AppUser
        {
            Id = Guid.NewGuid(),
            Email = email.Trim().ToLowerInvariant(),
            DisplayName = displayName.Trim(),
            PasswordHash = passwordHash,
            Role = role.Trim(),
            CreatedAt = now
        };
    }
}
