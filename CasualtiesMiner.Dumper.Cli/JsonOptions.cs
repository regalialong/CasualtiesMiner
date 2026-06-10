using CasualtiesMiner.Shared.Models;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace CasualtiesMiner.Dumper.Cli;

internal static class DumperJsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        AllowTrailingCommas = true,
        TypeInfoResolver = JsonTypeInfoResolver.Combine(JsonContext.Default),
        IncludeFields = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
}

[JsonSourceGenerationOptions(
    UseStringEnumConverter = true,
    IncludeFields = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(DumpedData))]
[JsonSerializable(typeof(ItemInfo))]
[JsonSerializable(typeof(LiquidItemInfo))]
[JsonSerializable(typeof(BatteryInfo))]
internal partial class JsonContext : JsonSerializerContext
{
}
