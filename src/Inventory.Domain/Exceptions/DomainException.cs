namespace Inventory.Domain.Exceptions;

public enum DomainErrorKind
{
    Validation,
    NotFound,
    Conflict,
    InsufficientStock,
    Unauthorized
}

public sealed class DomainException : Exception
{
    public DomainException(DomainErrorKind kind, string message)
        : base(message)
    {
        Kind = kind;
    }

    public DomainErrorKind Kind { get; }

    public static DomainException Validation(string message) => new(DomainErrorKind.Validation, message);

    public static DomainException NotFound(string message) => new(DomainErrorKind.NotFound, message);

    public static DomainException Conflict(string message) => new(DomainErrorKind.Conflict, message);

    public static DomainException InsufficientStock(string message) => new(DomainErrorKind.InsufficientStock, message);

    public static DomainException Unauthorized(string message) => new(DomainErrorKind.Unauthorized, message);
}
