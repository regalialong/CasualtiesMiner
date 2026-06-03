using System.Text.Json;

namespace CasualtiesMiner.Uploader.Data;

public sealed class LocaleCatalog
{
    public const string DefaultLanguageCode = "EN";

    public static LocaleCatalog Empty { get; } = new([], DefaultLanguageCode);

    public IReadOnlyList<GameLocale> Locales { get; }
    public string DefaultCode { get; }
    public GameLocale? Default { get; }

    private LocaleCatalog(IReadOnlyList<GameLocale> locales, string defaultCode)
    {
        Locales = locales;
        DefaultCode = defaultCode;
        Default = locales.FirstOrDefault(l => l.Code == defaultCode) ?? (locales[0] ?? null);
    }

    public static LocaleCatalog Load(string? localeDir, string? localeFile, string defaultCode = DefaultLanguageCode)
    {
        var locales = new List<GameLocale>();

        if (!string.IsNullOrWhiteSpace(localeDir) && Directory.Exists(localeDir))
        {
            foreach (var path in Directory.EnumerateFiles(localeDir, "*.json").OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
                locales.Add(ParseFile(path));
        }
        else if (!string.IsNullOrWhiteSpace(localeFile) && File.Exists(localeFile))
        {
            locales.Add(ParseFile(localeFile));
        }

        return new LocaleCatalog(locales, defaultCode.ToUpperInvariant());
    }

    private static GameLocale ParseFile(string path)
    {
        using var stream = File.OpenRead(path);
        using var document = JsonDocument.Parse(stream);

        var code = Path.GetFileNameWithoutExtension(path).ToUpperInvariant();
        var main = ReadStringDictionary(document.RootElement, "main");
        var other = ReadStringDictionary(document.RootElement, "other");

        return new GameLocale
        {
            Code = code,
            Main = main,
            Other = other
        };
    }

    private static Dictionary<string, string> ReadStringDictionary(JsonElement root, string propertyName)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        if (!root.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.Object)
        {
            return result;
        }

        foreach (var entry in element.EnumerateObject())
        {
            if (entry.Value.ValueKind == JsonValueKind.String)
            {
                result[entry.Name] = entry.Value.GetString() ?? string.Empty;
            }
        }

        return result;
    }
}
