using CasualtiesMiner.Shared.Models;
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
            PropertyNamingPolicy = namingPolicy
        };
    }
}

[JsonSourceGenerationOptions(
    UseStringEnumConverter = true,
    IncludeFields = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(DumpedData))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(byte))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(float))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(CraftingQuality))]
[JsonSerializable(typeof(CraftingQuality[]))]
[JsonSerializable(typeof(List<CraftingQuality>))]
[JsonSerializable(typeof(ItemInfo))]
[JsonSerializable(typeof(ItemInfo[]))]
[JsonSerializable(typeof(List<ItemInfo>))]
[JsonSerializable(typeof(LiquidItemInfo))]
[JsonSerializable(typeof(BatteryInfo))]
[JsonSerializable(typeof(LiquidStack))]
[JsonSerializable(typeof(LiquidStack[]))]
[JsonSerializable(typeof(List<LiquidStack>))]
[JsonSerializable(typeof(Color))]
[JsonSerializable(typeof(LiquidType))]
[JsonSerializable(typeof(LiquidType[]))]
[JsonSerializable(typeof(List<LiquidType>))]
[JsonSerializable(typeof(RecipeItem))]
[JsonSerializable(typeof(RecipeItem[]))]
[JsonSerializable(typeof(List<RecipeItem>))]
[JsonSerializable(typeof(RecipeResult))]
[JsonSerializable(typeof(Recipe))]
[JsonSerializable(typeof(Recipe[]))]
[JsonSerializable(typeof(List<Recipe>))]
[JsonSerializable(typeof(BlockInfo))]
[JsonSerializable(typeof(BlockInfo[]))]
[JsonSerializable(typeof(List<BlockInfo>))]
[JsonSerializable(typeof(MoodleInfo))]
[JsonSerializable(typeof(MoodleInfo[]))]
[JsonSerializable(typeof(List<MoodleInfo>))]
internal partial class JsonContext : JsonSerializerContext
{
}