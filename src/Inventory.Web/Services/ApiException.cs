using System.Net;
using System.Text.Json;

namespace Inventory.Web.Services;

public sealed class ApiException : Exception
{
    public ApiException(int status, string message, IReadOnlyDictionary<string, string[]> errors)
        : base(message)
    {
        Status = status;
        Errors = errors;
    }

    public int Status { get; }

    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public static async Task<ApiException> FromAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var status = (int)response.StatusCode;
        var title = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Sign in again to continue.",
            _ => $"The request failed ({status})."
        };
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            if (stream.CanSeek && stream.Length == 0)
            {
                return new ApiException(status, title, errors);
            }

            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = document.RootElement;
            if (root.TryGetProperty("title", out var titleProperty) && titleProperty.GetString() is { Length: > 0 } parsed)
            {
                title = parsed;
            }

            if (root.TryGetProperty("errors", out var errorsProperty) && errorsProperty.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in errorsProperty.EnumerateObject())
                {
                    if (property.Value.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    errors[property.Name] = property.Value.EnumerateArray()
                        .Select(item => item.GetString() ?? string.Empty)
                        .Where(item => item.Length > 0)
                        .ToArray();
                }
            }
        }
        catch (JsonException)
        {
        }

        return new ApiException(status, title, errors);
    }
}
