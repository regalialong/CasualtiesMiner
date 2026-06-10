using CasualtiesMiner.Shared.Models;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace CasualtiesMiner.Dumper.Cli;

internal sealed class DumpedData
{
    public ItemInfo[] Items { get; set; } = [];
    public Recipe[] Recipes { get; set; } = [];
    public LiquidType[] Liquids { get; set; } = [];
    public BlockInfo[] Tiles { get; set; } = [];
    public MoodleInfo[] Moodles { get; set; } = [];
}

internal static class DumperJsonOptions
{
    public static readonly JsonSerializerOptions CamelCaseOptions = CreateOptions(JsonNamingPolicy.CamelCase);

    public static readonly JsonSerializerOptions SnakeCaseLowerOptions = CreateOptions(JsonNamingPolicy.SnakeCaseLower);

    private static JsonSerializerOptions CreateOptions(JsonNamingPolicy namingPolicy)
    {
        return new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            TypeInfoResolver = JsonTypeInfoResolver.Combine(JsonContext.Default),
            IncludeFields = true,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            PropertyNamingPolicy = namingPolicy,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
    }
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