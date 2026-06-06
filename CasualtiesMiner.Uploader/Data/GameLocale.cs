namespace CasualtiesMiner.Uploader.Data;

public sealed class GameLocale
{
    public string FileName { get; init; } = string.Empty;

    public required string Code { get; init; }

    public IReadOnlyDictionary<string, string> Main { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> Other { get; init; } = new Dictionary<string, string>();

    public string GetObjectName(string id)
    {
        return Main.TryGetValue(id, out var name) && !string.IsNullOrWhiteSpace(name) ? name : id;
    }

    public string GetObjectDescription(string id)
    {
        return Main.TryGetValue(id + "dsc", out var description) ? description : string.Empty;
    }

    public string GetOther(string key, string fallback)
    {
        return Other.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
    }
}
