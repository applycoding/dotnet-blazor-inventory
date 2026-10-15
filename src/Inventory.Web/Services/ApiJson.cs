using System.Text.Json;
using System.Text.Json.Serialization;

namespace Inventory.Web.Services;

public static class ApiJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
