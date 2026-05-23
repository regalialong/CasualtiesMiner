using CasualtiesMiner.Shared.Models;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace CasualtiesMiner.Dumper.Cli;

internal static class DumperJsonOptions
{
    private static readonly ObjectJsonConverter ObjectConverter = new();

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
            Converters = { ObjectConverter }
        };
    }
}

internal sealed class ObjectJsonConverter : JsonConverter<object?>
{
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }

    public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case bool boolean:
                writer.WriteBooleanValue(boolean);
                break;
            case byte byteValue:
                writer.WriteNumberValue(byteValue);
                break;
            case sbyte sbyteValue:
                writer.WriteNumberValue(sbyteValue);
                break;
            case short shortValue:
                writer.WriteNumberValue(shortValue);
                break;
            case ushort ushortValue:
                writer.WriteNumberValue(ushortValue);
                break;
            case int intValue:
                writer.WriteNumberValue(intValue);
                break;
            case uint uintValue:
                writer.WriteNumberValue(uintValue);
                break;
            case long longValue:
                writer.WriteNumberValue(longValue);
                break;
            case ulong ulongValue:
                writer.WriteNumberValue(ulongValue);
                break;
            case float floatValue:
                writer.WriteNumberValue(floatValue);
                break;
            case double doubleValue:
                writer.WriteNumberValue(doubleValue);
                break;
            case decimal decimalValue:
                writer.WriteNumberValue(decimalValue);
                break;
            case string stringValue:
                writer.WriteStringValue(stringValue);
                break;
            case string[] strings:
                writer.WriteStartArray();
                foreach (var entry in strings) writer.WriteStringValue(entry);
                writer.WriteEndArray();
                break;
            case ItemInfo[] itemInfos:
                JsonSerializer.Serialize(writer, itemInfos, JsonContext.Default.ItemInfoArray);
                break;
            case RecipeInfo[] recipeInfos:
                JsonSerializer.Serialize(writer, recipeInfos, JsonContext.Default.RecipeInfoArray);
                break;
            case LiquidType[] liquidTypes:
                JsonSerializer.Serialize(writer, liquidTypes, JsonContext.Default.LiquidTypeArray);
                break;
            case BlockInfo[] blockInfos:
                JsonSerializer.Serialize(writer, blockInfos, JsonContext.Default.BlockInfoArray);
                break;
            case Array array:
                writer.WriteStartArray();
                foreach (var element in array) Write(writer, element, options);
                writer.WriteEndArray();
                break;
            default:
                throw new NotSupportedException($"Unsupported JSON value type: {value.GetType().FullName}");
        }
    }
}

[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
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
[JsonSerializable(typeof(RecipeInfo))]
[JsonSerializable(typeof(RecipeInfo[]))]
[JsonSerializable(typeof(List<RecipeInfo>))]
[JsonSerializable(typeof(BlockInfo))]
[JsonSerializable(typeof(BlockInfo[]))]
[JsonSerializable(typeof(List<BlockInfo>))]
[JsonSerializable(typeof(ConcurrentDictionary<string, object>))]
internal partial class JsonContext : JsonSerializerContext
{
}