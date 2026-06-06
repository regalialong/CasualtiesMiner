namespace CasualtiesMiner.Uploader.Data;

/// <summary>
/// A flattened, wiki-ready representation of a single liquid entity. All values are already converted to the
/// shape expected by the Bucket schema; the Lua/wikitext generators only serialize this object.
/// </summary>
public sealed record LiquidRow
{
    public required string LiquidId { get; init; }

    /// <summary>
    /// Stable, language-neutral wiki title (<c>Item:bandage</c>).
    /// </summary>
    public string PageTitle => "Liquid:" + LiquidId;

    public required string Color { get; init; }

    public required float ValuePerLiter { get; init; }
    public required float InjectionSickness { get; init; }

    public required bool HealthUsable { get; init; }
    public required bool Injectable { get; init; }
    public required bool LocaleFromItem { get; init; }

    public required IReadOnlyList<string> Qualities { get; init; }
}
