namespace Stockroom.Infrastructure.Persistence;

public static class DatabaseProviders
{
    public const string Sqlite = "Sqlite";

    public const string SqlServer = "SqlServer";

    public static string Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals(Sqlite, StringComparison.OrdinalIgnoreCase))
        {
            return Sqlite;
        }

        if (value.Equals(SqlServer, StringComparison.OrdinalIgnoreCase))
        {
            return SqlServer;
        }

        throw new InvalidOperationException($"DatabaseProvider '{value}' is not supported. Use Sqlite or SqlServer.");
    }
}
