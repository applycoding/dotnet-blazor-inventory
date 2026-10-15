using System.Text;

using Microsoft.Extensions.Configuration;

namespace Stockroom.Application.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "stockroom";

    public string Audience { get; set; } = "stockroom";

    public string Key { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 480;

    public static JwtOptions Read(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        var options = new JwtOptions
        {
            Issuer = section["Issuer"] ?? "stockroom",
            Audience = section["Audience"] ?? "stockroom",
            Key = section["Key"] ?? string.Empty,
            ExpiryMinutes = int.TryParse(section["ExpiryMinutes"], out var minutes) ? minutes : 480
        };

        if (string.IsNullOrWhiteSpace(options.Issuer) || string.IsNullOrWhiteSpace(options.Audience))
        {
            throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience are required.");
        }

        if (string.IsNullOrWhiteSpace(options.Key) || Encoding.UTF8.GetByteCount(options.Key) < 32)
        {
            throw new InvalidOperationException("Jwt:Key must be at least 32 UTF-8 bytes.");
        }

        if (options.ExpiryMinutes < 5)
        {
            throw new InvalidOperationException("Jwt:ExpiryMinutes must be at least 5.");
        }

        return options;
    }
}
