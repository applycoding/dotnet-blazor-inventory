namespace Stockroom.Domain.Common;

public static class OrderNumber
{
    public static string Format(int sequence)
    {
        if (sequence < 1)
        {
            throw new DomainException("Order sequence must be positive.");
        }

        return $"SR-{DateTime.UtcNow:yyyy}-{sequence:0000}";
    }
}
