using Stockroom.Domain.Common;

namespace Stockroom.Domain.Entities;

public sealed class Customer
{
    private Customer()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string? Phone { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static Customer Create(string name, string email, string? phone, DateTimeOffset? createdAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Customer name is required.");
        }

        var trimmedName = name.Trim();
        if (trimmedName.Length > 160)
        {
            throw new DomainException("Customer name cannot be longer than 160 characters.");
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.Ordinal))
        {
            throw new DomainException("Customer email is required.");
        }

        var trimmedEmail = email.Trim().ToLowerInvariant();
        if (trimmedEmail.Length > 200)
        {
            throw new DomainException("Customer email cannot be longer than 200 characters.");
        }

        string? trimmedPhone = null;
        if (!string.IsNullOrWhiteSpace(phone))
        {
            trimmedPhone = phone.Trim();
            if (trimmedPhone.Length > 40)
            {
                throw new DomainException("Phone cannot be longer than 40 characters.");
            }
        }

        return new Customer
        {
            Id = Guid.NewGuid(),
            Name = trimmedName,
            Email = trimmedEmail,
            Phone = trimmedPhone,
            CreatedAtUtc = createdAtUtc ?? DateTimeOffset.UtcNow
        };
    }
}
